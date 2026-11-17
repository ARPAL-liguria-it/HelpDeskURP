/*
 * Nome del progetto: HelpDesk URP
 * Copyright (C) 2026 Agenzia regionale per la protezione dell'ambiente ligure
 * ---
 * Questo programma è software libero: puoi ridistribuirlo e/o modificarlo
 * secondo i termini della GNU Affero General Public License pubblicata dalla
 * Free Software Foundation, sia la versione 3 della licenza, sia (a tua scelta)
 * qualsiasi versione successiva.
 * ---
 * Questo programma è distribuito nella speranza che possa essere utile,
 * ma SENZA ALCUNA GARANZIA; senza nemmeno la garanzia implicita di
 * COMMERCIABILITÀ o IDONEITÀ PER UNO SCOPO PARTICOLARE. Vedi la
 * GNU Affero General Public License per ulteriori dettagli.
 * ---
 * Dovresti aver ricevuto una copia della GNU Affero General Public License
 * insieme a questo programma. In caso contrario, vedi <https://www.gnu.org/licenses/>.
*/

using Microsoft.AspNetCore.Components.Server.Circuits;

namespace GestioneURP.Infrastructure.Email;

public sealed class EmailCircuitHandler : CircuitHandler, IDisposable
{
	private readonly object _gate = new();
	private CancellationTokenSource _connessione = new();
	private bool _disposed;

	public CancellationTokenSource CreaOperazione(CancellationToken componente)
	{
		lock (_gate)
			return CancellationTokenSource.CreateLinkedTokenSource(_connessione.Token, componente);
	}

	public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
	{
		lock (_gate) _connessione.Cancel();
		return Task.CompletedTask;
	}

	public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
	{
		lock (_gate)
		{
			if (_connessione.IsCancellationRequested)
			{
				_connessione.Dispose();
				_connessione = new CancellationTokenSource();
			}
		}
		return Task.CompletedTask;
	}

	public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
		=> OnConnectionDownAsync(circuit, cancellationToken);

	public void Dispose()
	{
		lock (_gate)
		{
			if (_disposed) return;
			_disposed = true;
			_connessione.Cancel();
			_connessione.Dispose();
		}
	}
}
