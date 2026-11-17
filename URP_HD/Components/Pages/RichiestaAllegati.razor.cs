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

using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace URP_HD.Components.Pages
{
	public partial class RichiestaAllegati
	{
		[Parameter]
		public RichiestaWizardState State { get; set; } = default!;
		[Parameter]
		public EventCallback OnAvanti { get; set; }

		[Parameter]
		public EventCallback OnIndietro { get; set; }

		FluentInputFileEventArgs[] Files = Array.Empty<FluentInputFileEventArgs>();

		private List<string> elencoErrori = new();

		void OnCompleted(IEnumerable<FluentInputFileEventArgs> files)
		{
			elencoErrori.Clear();
			Files = files.ToArray();

			foreach (var file in Files)
			{
				if (file.Size > 10 * 1024 * 1024) // 10 MB
				{
					elencoErrori.Add($"File {file.Name} non caricato: supera la dimensione massima consentita.");
					if (File.Exists(file.LocalFile?.FullName))
					{
						File.Delete(file.LocalFile?.FullName);
						continue;
					}
				}
				if (State.AllegatiCaricati.Count >= 10)
				{
					elencoErrori.Add($"File {file.Name} non caricato: è possibile caricare solo fino a 10 file.");
					if (File.Exists(file.LocalFile?.FullName))
					{
						File.Delete(file.LocalFile?.FullName);
					}
					continue;
				}
				var nuovoAllegato = new RichiestaWizardState.AllegatoCaricato
				{
					NomeFile = file.Name,
					NomeFileTemp = file.LocalFile?.Name ?? string.Empty,
					DimensioneFile = file.Size
				};
				State.AllegatiCaricati.Add(nuovoAllegato);
			}
		}

		private void EliminaAllegato(RichiestaWizardState.AllegatoCaricato allegato)
		{
			var percorsoFileDaEliminare = Path.Combine(Configuration["CartellaTemporanea"], allegato.NomeFileTemp);
			if (File.Exists(percorsoFileDaEliminare))
			{
				File.Delete(percorsoFileDaEliminare);
			}
			State.AllegatiCaricati.Remove(allegato);

			StateHasChanged();
		}

		private async Task Avanti()
		{
			if (State.Step < 4)
			{
				State.Step++;

				await OnAvanti.InvokeAsync();
			}
		}

		private async Task Indietro()
		{
			if (State.Step > 1)
			{
				State.Step--;

				await OnIndietro.InvokeAsync();
			}
		}
	}

}
