using NBitcoin;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WalletWasabi.BitcoinCore;
using WalletWasabi.Models;
using WalletWasabi.Services;
using WalletWasabi.Stores;
using WalletWasabi.Tests.Helpers;
using Xunit;

namespace WalletWasabi.Tests.UnitTests.BitcoinCore
{
	public class RpcBasedTests
	{
		[Fact]
		public async Task AllFeeEstimateAsync()
		{
			using var services = new HostedServices();
			var coreNode = await TestNodeBuilder.CreateAsync(services);
			await services.StartAllAsync(CancellationToken.None);
			try
			{
				var rpc = coreNode.RpcClient;
				var estimations = await rpc.EstimateAllFeeAsync(EstimateSmartFeeMode.Conservative, simulateIfRegTest: true, tolerateBitcoinCoreBrainfuck: true);
				Assert.Equal(WalletWasabi.Helpers.Constants.OneDayConfirmationTarget, estimations.Estimations.Count);
				Assert.True(estimations.Estimations.First().Key < estimations.Estimations.Last().Key);
				Assert.True(estimations.Estimations.First().Value > estimations.Estimations.Last().Value);
				Assert.Equal(EstimateSmartFeeMode.Conservative, estimations.Type);
				estimations = await rpc.EstimateAllFeeAsync(EstimateSmartFeeMode.Economical, simulateIfRegTest: true, tolerateBitcoinCoreBrainfuck: true);
				Assert.Equal(145, estimations.Estimations.Count);
				Assert.True(estimations.Estimations.First().Key < estimations.Estimations.Last().Key);
				Assert.True(estimations.Estimations.First().Value > estimations.Estimations.Last().Value);
				Assert.Equal(EstimateSmartFeeMode.Economical, estimations.Type);
				estimations = await rpc.EstimateAllFeeAsync(EstimateSmartFeeMode.Economical, simulateIfRegTest: true, tolerateBitcoinCoreBrainfuck: false);
				Assert.Equal(145, estimations.Estimations.Count);
				Assert.True(estimations.Estimations.First().Key < estimations.Estimations.Last().Key);
				Assert.True(estimations.Estimations.First().Value > estimations.Estimations.Last().Value);
				Assert.Equal(EstimateSmartFeeMode.Economical, estimations.Type);
			}
			finally
			{
				await services.StopAllAsync(CancellationToken.None);
				await coreNode.TryStopAsync();
			}
		}

		[Fact]
		public async Task FetchBlockFilterIndexFromCoreAsync()
		{
			using var services = new HostedServices();
			var coreNode = await TestNodeBuilder.CreateAsync(services, blockFilterIndex: "basic");
			await services.StartAllAsync(CancellationToken.None);
			try
			{
				var rpc = coreNode.RpcClient;
				var blockFilter = await rpc.GetBlockFilterAsync(Network.RegTest.GenesisHash);

				var genesis = Network.RegTest.GetGenesis();
				var genesisScriptPubKey = genesis.Transactions[0].Outputs[0].ScriptPubKey;
				Assert.True(blockFilter.Filter.Match(genesisScriptPubKey.ToBytes(), Network.RegTest.GenesisHash.ToBytes()));
				Assert.False(blockFilter.Filter.Match(genesisScriptPubKey.ToBytes().Skip(1).ToArray(), Network.RegTest.GenesisHash.ToBytes()));

				// Compare the ScriptPubKey with filters generated by wasabi params.
				GolombRiceFilter wasabiFilter = new GolombRiceFilterBuilder()
					.SetKey(Network.RegTest.GenesisHash)
					.SetP(20)
					.SetM(1 << 20)
					.AddEntries(new[] { genesis.Transactions[0].Outputs.First().ScriptPubKey.ToBytes() })
					.Build();

				Assert.True(wasabiFilter.Match(genesisScriptPubKey.ToBytes(), Network.RegTest.GenesisHash.ToBytes()));
				Assert.False(wasabiFilter.Match(genesisScriptPubKey.ToBytes().Skip(1).ToArray(), Network.RegTest.GenesisHash.ToBytes()));
			}
			finally
			{
				await services.StopAllAsync(CancellationToken.None);
				await coreNode.TryStopAsync();
			}
		}
	}
}
