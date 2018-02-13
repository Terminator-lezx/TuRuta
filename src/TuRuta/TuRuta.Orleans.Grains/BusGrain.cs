﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Orleans;
using Orleans.Streams;
using Orleans.Providers;

using TuRuta.Orleans.Grains.Services.Interfaces;
using TuRuta.Orleans.Grains.Services;
using TuRuta.Orleans.Interfaces;
using TuRuta.Common.Device;
using TuRuta.Common.StreamObjects;
using TuRuta.Orleans.Grains.States;

namespace TuRuta.Orleans.Grains
{
    [StorageProvider(ProviderName = "AzureTableStore")]
    [ImplicitStreamSubscription("Buses")]
    public class BusGrain : Grain<BusState>, IBusGrain
    {
        private IAsyncStream<PositionUpdate> injestionStream;
		private IClientUpdate clientUpdate;
		private Queue<PositionUpdate> notSentUpdates = new Queue<PositionUpdate>();

        public async override Task OnActivateAsync()
        {
			clientUpdate = new PubNubClientUpdate();
            var streamProvider = GetStreamProvider("StreamProvider");
            injestionStream = streamProvider.GetStream<PositionUpdate>(this.GetPrimaryKey(), "Buses");
            await injestionStream.SubscribeAsync(async (message, token) =>
            {
				var sentTask = clientUpdate.SentUpdate(new ClientBusUpdate {
					Latitude = message.Latitude,
					Longitude = message.Longitude,
					BusId = this.GetPrimaryKey()
				});

				State.CurrentLatitude = message.Latitude;
				State.CurrentLongitude = message.Longitude;

				await sentTask;
			});

            await base.OnActivateAsync();
        }
    }
}
