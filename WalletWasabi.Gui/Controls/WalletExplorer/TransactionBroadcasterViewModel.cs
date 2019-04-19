﻿using Avalonia;
using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace WalletWasabi.Gui.Controls.WalletExplorer
{
	public class TransactionBroadcasterViewModel : WalletActionViewModel
	{
		private string _transactionString;
		private string _errorMessage;
		private string _successMessage;
		private bool _isBusy;
		private string _buttonText;

		private CompositeDisposable Disposables { get; set; }
		public ReactiveCommand<Unit, Unit> PasteCommand { get; set; }
		public ReactiveCommand<Unit, Unit> BroadcastTransactionCommand { get; set; }

		public string TransactionString
		{
			get => _transactionString;
			set => this.RaiseAndSetIfChanged(ref _transactionString, value);
		}

		public string ErrorMessage
		{
			get => _errorMessage;
			set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
		}

		public string SuccessMessage
		{
			get => _successMessage;
			set => this.RaiseAndSetIfChanged(ref _successMessage, value);
		}

		public bool IsBusy
		{
			get => _isBusy;
			set => this.RaiseAndSetIfChanged(ref _isBusy, value);
		}

		public string ButtonText
		{
			get => _buttonText;
			set => this.RaiseAndSetIfChanged(ref _buttonText, value);
		}

		public TransactionBroadcasterViewModel(WalletViewModel walletViewModel) : base("Transaction Broadcaster", walletViewModel)
		{
			ButtonText = "Broadcast Transaction";

			PasteCommand = ReactiveCommand.CreateFromTask(async () =>
			{
				if (!string.IsNullOrEmpty(TransactionString))
				{
					return;
				}

				var textToPaste = await Application.Current.Clipboard.GetTextAsync();
				TransactionString = textToPaste;
			});

			BroadcastTransactionCommand = ReactiveCommand.CreateFromTask(async () =>
			{
				await OnDoTransactionBroadcastAsync();
			});

			Observable.Merge(PasteCommand.ThrownExceptions)
				.Merge(BroadcastTransactionCommand.ThrownExceptions)
				.Subscribe(OnException);
		}

		private void OnException(Exception ex)
		{
			ErrorMessage = ex.ToTypeMessageString();
		}

		public override void OnOpen()
		{
			if (Disposables != null)
			{
				throw new Exception("TransactionBroadcaster was opened before it was closed.");
			}

			Disposables = new CompositeDisposable();

			this.WhenAnyValue(x => x.TransactionString)
				.Throttle(TimeSpan.FromSeconds(1))
				.Subscribe(async (text) => await OnTransactionTextChangedAsync(text));

			base.OnOpen();
		}

		public override bool OnClose()
		{
			Disposables?.Dispose();
			Disposables = null;

			TransactionString = "";

			return base.OnClose();
		}

		private async Task OnTransactionTextChangedAsync(string text)
		{
			try
			{
				if (string.IsNullOrEmpty(text))
				{
					return;
				};

				await BroadcastTransactionCommand.Execute();
			}
			catch (Exception ex)
			{
				OnException(ex);
			}
		}

		private async Task OnDoTransactionBroadcastAsync()
		{
			try
			{
				IsBusy = true;
				ButtonText = "Broadcasting Transaction...";

				// TODO: Parse transaction and broadcast it.

				await Task.Delay(3000);
			}
			catch (Exception ex)
			{
				OnException(ex);
			}
			finally
			{
				IsBusy = false;
				ButtonText = "Broadcast Transaction";
			}
		}
	}
}
