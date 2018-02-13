﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Orleans;
using Orleans.Streams;
using TuRuta.Orleans.Grains.Services.Interfaces;
using TuRuta.Orleans.Grains.Services;
using TuRuta.Orleans.Interfaces;
using TuRuta.Common.Device;
using TuRuta.Orleans.Grains.States;
using Orleans.Providers;
using TuRuta.Common.Models;

namespace TuRuta.Orleans.Grains
{
	[StorageProvider(ProviderName = "AzureTableStore")]
	[ImplicitStreamSubscription("Rutas")]
	public class RutaGrain : Grain<RutaState>, IRutaGrain
    {
		private IAsyncStream<Object> injestionStreamParada;

		public Task<List<Parada>> AllParadas()
		{
			return Task.FromResult(State.AllParadas);
		}

		public async override Task OnActivateAsync()
		{
			var streamProvider = GetStreamProvider("StreamProvider");
			injestionStreamParada = streamProvider.GetStream<Object>(this.GetPrimaryKey(), "Rutas");
			await base.OnActivateAsync();
		}
    }
}
