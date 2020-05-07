﻿using System;

using MLIDS.lib.ML.Objects;

using PacketDotNet;

using SharpPcap;
using SharpPcap.Npcap;

namespace MLIDS.lib.Windows.ViewModels
{
    public abstract class BaseCaptureMainViewModel : BaseViewModel
    {
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        private bool _chkBxSaveEnabled;

        public bool ChkBxSaveEnabled
        {
            get => _chkBxSaveEnabled;

            set
            {
                _chkBxSaveEnabled = value;

                OnPropertyChanged();
            }
        }

        public BaseCaptureMainViewModel()
        {
            ChkBxSaveEnabled = true;
        }

        public override void StartAction()
        {
            StartBtnEnabled = false;
            StopBtnEnabled = true;
            DeviceSelectionEnabled = false;
            ChkBxSaveEnabled = false;

            if (SelectedDevice is NpcapDevice)
            {
                var nPcap = SelectedDevice as NpcapDevice;

                nPcap?.Open(OpenFlags.DataTransferUdp | OpenFlags.NoCaptureLocal, 1000);
            }

            SelectedDevice.OnPacketArrival += Device_OnPacketArrival;
            SelectedDevice.StartCapture();
        }

        public override void StopAction()
        {
            try
            {
                SelectedDevice.OnPacketArrival -= Device_OnPacketArrival;

                SelectedDevice.StopCapture();
                SelectedDevice.Close();
            }
            catch (Exception) { }

            StartBtnEnabled = true;
            StopBtnEnabled = false;
            DeviceSelectionEnabled = true;
            ChkBxSaveEnabled = true;
        }

        public abstract void PacketProcessing(PayloadItem payloadItem);

        private void Device_OnPacketArrival(object sender, CaptureEventArgs e)
        {
            if (e == null)
            {
                Log.Error("MainViewModel::PacketProcessing - e is null");

                throw new ArgumentNullException(nameof(e));
            }

            System.Windows.Application.Current.Dispatcher.Invoke(delegate
            {
                var packet = Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);

                if (!packet.HasPayloadPacket)
                {
                    Log.Info("Packet has no payload");

                    return;
                }

                var packetItem = GetPacket(packet, false);

                if (packetItem == null)
                {
                    Log.Info("PacketItem was null");

                    return;
                }

                PacketProcessing(packetItem);
            });
        }
    }
}