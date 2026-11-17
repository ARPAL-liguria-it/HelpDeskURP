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

using GestioneURP.Application.Services;
using GestioneURP.Components.State;
using GestioneURP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.FluentUI.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;


namespace GestioneURP.Components.Pages;

// Gestisce il flusso in due passaggi: compilazione e conferma prima della creazione.
public partial class NuovaSegnalazione
{
	// Il componente invoca direttamente i servizi lato server e usa lo stato dell'operatore per l'accesso.
	[Inject] private SegnalazioniService SegnalazioniService { get; set; } = default!;
	[Inject] private OperatoreState OperatoreCorrente { get; set; } = default!;
	[Inject] private NavigationManager NavigationManager { get; set; } = default!;
	[Inject] private IConfiguration Configuration { get; set; } = default!;
	[Inject] private ILogger<NuovaSegnalazione> Logger { get; set; } = default!;
	[Inject] private UrpHdDbContext DbContext { get; set; } = default!;

	// Modello, selezioni e allegati restano nel componente anche passando al riepilogo e tornando al form.
	private NuovaSegnalazioneLookupDto _lookup = new([], [], [], [], [], []);
	private NuovaSegnalazioneModel _model = new();
	private List<string> _comuni = [];
	private List<string> _comuniFiltrati = [];
	private IEnumerable<string> _comuneLuogoSelezionato = [];
	private EditContext _editContext = default!;
	private List<AllegatoNuovoStaging> _nuoviAllegati = [];
	private int _step = 1;
	private bool _inCaricamento = true;
	private bool _inSalvataggio;
	// La versione consente al markup di ricreare l'avviso a ogni tentativo di validazione fallito.
	private bool _mostraAvvisoValidazione;
	private int _versioneAvvisoValidazione;
	private string? _erroreGenerale;
	private string? _erroreAllegati;
	// Queste condizioni pilotano i campi visibili e il contenuto del riepilogo.
	private bool IsRichiesta => string.Equals(_model.TipoSegnalazione, "richiesta", StringComparison.OrdinalIgnoreCase);
	private bool IsReclamo => string.Equals(_model.TipoSegnalazione, "reclamo", StringComparison.OrdinalIgnoreCase);
	private bool MostraComuneSpecifico => string.Equals(_model.DescrizioneLuogoSelezionata, "Comune specifico", StringComparison.Ordinal);

	protected override async Task OnInitializedAsync()
	{
		// L'EditContext mantiene i messaggi di validazione associati al modello del form.
		_editContext = new EditContext(_model);

		// L'accesso è riservato ai gestori; gli altri operatori vengono indirizzati alla pagina di errore.
		if (!OperatoreCorrente.SeGestore())
		{
			var messaggio = $"L'utente {OperatoreCorrente.UserName} non è autorizzato ad accedere alla pagina.";
			NavigationManager.NavigateTo($"/errore-logon?testo={Uri.EscapeDataString(messaggio)}");
			return;
		}

		try
		{
			// Carica le opzioni dei campi e un elenco ordinato di comuni, senza tracciare le entità EF.
			_lookup = await SegnalazioniService.GetLookupNuovaSegnalazioneAsync();
			_comuni = await DbContext.ElencoComuni
				.AsNoTracking()
				.Where(x => !string.IsNullOrWhiteSpace(x.Descrizione))
				.Select(x => x.Descrizione.Trim())
				.Distinct()
				.OrderBy(x => x)
				.ToListAsync();
			_comuniFiltrati = _comuni;
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Errore durante il caricamento dei dati per la nuova segnalazione");
			_erroreGenerale = "Impossibile caricare i dati iniziali.";
		}
		finally
		{
			_inCaricamento = false;
		}
	}

	private Task OnTipoSegnalazioneChanged()
	{
		// Rimuove gli avvisi precedenti e riallinea la descrizione del luogo senza azzerare i dati inseriti.
		_erroreGenerale = null;
		_mostraAvvisoValidazione = false;
		OnLuogoChanged();
		return Task.CompletedTask;
	}

	private Task OnComuniSearch(OptionsSearchEventArgs<string> e)
	{
		// La ricerca usa il prefisso, ignorando maiuscole/minuscole, e limita a 20 i risultati non vuoti.
		var testo = e.Text ?? string.Empty;
		var items = string.IsNullOrWhiteSpace(testo)
			? _comuni
			: _comuni
				.Where(x => x.StartsWith(testo, StringComparison.OrdinalIgnoreCase))
				.Take(20)
				.ToList();
		e.Items = items;
		_comuniFiltrati = items;
		return Task.CompletedTask;
	}

	private Task OnComuneLuogoSelezionatoChanged(IEnumerable<string>? valore)
	{
		// L'autocomplete restituisce una collezione: il form usa solo il primo comune selezionato.
		_comuneLuogoSelezionato = valore ?? [];
		_model.ComuneLuogo = _comuneLuogoSelezionato.FirstOrDefault();
		// La selezione non usa il binding del campo: notifica esplicitamente la modifica all'EditContext.
		_editContext.NotifyFieldChanged(new FieldIdentifier(_model, nameof(_model.ComuneLuogo)));
		return Task.CompletedTask;
	}

	// Espone gli errori del campo per lo stile e l'attributo di accessibilità dell'autocomplete.
	private string? GetFieldValidationClass(string propertyName)
	{
		var field = new FieldIdentifier(_model, propertyName);
		return _editContext.GetValidationMessages(field).Any() ? "input-validation-error" : null;
	}

	private bool GetFieldHasError(string propertyName)
	{
		var field = new FieldIdentifier(_model, propertyName);
		return _editContext.GetValidationMessages(field).Any();
	}

	private Task OnLuogoChanged()
	{
		// La descrizione determina sia la visibilità sia l'obbligatorietà del comune specifico.
		_model.DescrizioneLuogoSelezionata = GetDescrizioneLong(_lookup.TipiLuogo, _model.IdTipoLuogo);
		return Task.CompletedTask;
	}

	private Task VaiAConfermaAsync()
	{
		_erroreGenerale = null;
		// Valida attributi e regole condizionali del modello prima di mostrare il riepilogo; non salva dati.
		if (!_editContext.Validate())
		{
			_mostraAvvisoValidazione = true;
			_versioneAvvisoValidazione++;
			return Task.CompletedTask;
		}

		_mostraAvvisoValidazione = false;
		_step = 2;
		return Task.CompletedTask;
	}

	// Torna alla compilazione mantenendo il modello e gli allegati già caricati.
	private void TornaAlForm()
	{
		_erroreGenerale = null;
		_mostraAvvisoValidazione = false;
		_step = 1;
	}

	private async Task SalvaAsync()
	{
		_erroreGenerale = null;
		// Durante il salvataggio il markup disabilita i pulsanti del riepilogo.
		_inSalvataggio = true;

		try
		{
			// Converte le selezioni testuali in identificativi e normalizza i campi vuoti a null per il servizio.
			var input = new NuovaSegnalazioneInputDto(
				TipoSegnalazione: _model.TipoSegnalazione ?? string.Empty,
				IdTipoRichiesta: ParseLong(_model.IdTipoRichiesta),
				Sorgente: PulisciValore(_model.Sorgente),
				Cognome: PulisciValore(_model.Cognome),
				Nome: PulisciValore(_model.Nome),
				Email: PulisciValore(_model.Email),
				Pec: PulisciValore(_model.Pec),
				Telefono: PulisciValore(_model.Telefono),
				RagioneSociale: PulisciValore(_model.RagioneSociale),
				IdTipoAutoreRichiesta: ParseLong(_model.IdTipoAutoreRichiesta),
				IdTipoTematica: ParseLong(_model.IdTipoTematica),
				IdTipoLuogo: ParseLong(_model.IdTipoLuogo),
				ComuneLuogo: PulisciValore(_model.ComuneLuogo),
				Indirizzo: PulisciValore(_model.Indirizzo),
				Testo: PulisciValore(_model.Testo),
				CodiceStruttura: PulisciValore(_model.CodiceStruttura));

			// Solo la conferma finale invia al servizio i dati della segnalazione e i contenuti degli allegati.
			var id = await SegnalazioniService.CreaNuovaSegnalazioneAsync(
				input,
				_nuoviAllegati.Select(x => new AllegatoNuovoUploadDto(x.NomeFile, x.Contenuto)).ToList());

			// Dopo la creazione apre il dettaglio della nuova segnalazione.
			var tipo = IsRichiesta ? "richiesta" : "reclamo";
			NavigationManager.NavigateTo($"/segnalazioni/dettaglio/{id}?tipo={tipo}");
		}
		catch (InvalidOperationException ex)
		{
			// Gli errori bloccanti espongono il messaggio del servizio; quelli inattesi hanno un messaggio generico.
			_erroreGenerale = ex.Message;
			Logger.LogWarning(ex, "Errore bloccante durante il salvataggio nuova segnalazione");
		}
		catch (Exception ex)
		{
			_erroreGenerale = "Errore durante il salvataggio. Riprovare.";
			Logger.LogError(ex, "Errore inatteso durante il salvataggio nuova segnalazione");
		}
		finally
		{
			_inSalvataggio = false;
		}
	}

	private async Task AggiungiAllegati(InputFileChangeEventArgs e)
	{
		_erroreAllegati = null;

		foreach (var file in e.GetMultipleFiles())
		{
			// Scarta il singolo file non valido ma continua a elaborare gli altri file selezionati.
			var errore = ValidaNuovoAllegato(file);
			if (errore is not null)
			{
				_erroreAllegati = errore;
				continue;
			}

			// Mantiene i byte in memoria fino al salvataggio, applicando il limite anche alla lettura dello stream.
			await using var stream = file.OpenReadStream(GetDimensioneMassimaBytes());
			using var memory = new MemoryStream();
			await stream.CopyToAsync(memory);
			_nuoviAllegati.Add(new AllegatoNuovoStaging(Guid.NewGuid(), NormalizzaNomeFile(file.Name), file.Size, memory.ToArray()));
		}
	}

	// Rimuove solo l'allegato temporaneo dal componente, senza intervenire sulla persistenza.
	private void RimuoviNuovoAllegato(Guid id) => _nuoviAllegati.RemoveAll(x => x.Id == id);

	// Restituisce il primo errore relativo a estensione, dimensione, numero di file o nome duplicato.
	private string? ValidaNuovoAllegato(IBrowserFile file)
	{
		var nomeFile = NormalizzaNomeFile(file.Name);
		var estensione = Path.GetExtension(nomeFile);
		if (!GetEstensioniConsentite().Contains(estensione))
			return "L'estensione del file non è consentita.";

		if (file.Size > GetDimensioneMassimaBytes())
			return "La dimensione del file supera il massimo consentito.";

		if (_nuoviAllegati.Count + 1 > GetNumeroMassimoAllegati())
			return "È stato superato il numero massimo di allegati consentito.";

		if (_nuoviAllegati.Any(x => string.Equals(x.NomeFile, nomeFile, StringComparison.OrdinalIgnoreCase)))
			return "Esiste già un allegato con lo stesso nome file.";

		return null;
	}

	private HashSet<string> GetEstensioniConsentite()
	{
		// Uniforma le estensioni configurate aggiungendo il punto iniziale; se assenti usa l'elenco predefinito.
		var valori = Configuration.GetSection("EstensioniAllegati").GetChildren()
			.Select(x => x.Value)
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Select(x => x!.Trim().StartsWith('.') ? x!.Trim().ToLowerInvariant() : "." + x!.Trim().ToLowerInvariant())
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

		return valori.Count > 0
			? valori
			: new HashSet<string>([".pdf", ".png", ".jpg", ".jpeg", ".doc", ".docx"], StringComparer.OrdinalIgnoreCase);
	}

	// I limiti provengono dalla configurazione; valori assenti o non validi usano 10 MB e 10 allegati.
	private long GetDimensioneMassimaBytes()
		=> int.TryParse(Configuration["DimensioneMassimaAllegatoMB"], out var megabyte) && megabyte > 0
			? megabyte * 1024L * 1024L
			: 10 * 1024L * 1024L;

	private int GetNumeroMassimoAllegati()
		=> int.TryParse(Configuration["NumeroMassimoAllegati"], out var massimo) && massimo > 0 ? massimo : 10;

	// Il filtro del selettore file aiuta l'utente, ma non sostituisce la validazione effettuata al caricamento.
	private string GetAcceptAttribute() => string.Join(',', GetEstensioniConsentite());

	// Conserva solo il nome del file, eliminando eventuali componenti di percorso riconosciute da Path.
	private static string NormalizzaNomeFile(string nomeFile) => Path.GetFileName(nomeFile.Trim());
	// Funzioni di presentazione per dimensioni e valori mancanti e di conversione per l'input del servizio.
	private static string FormattaDimensione(long bytes) => bytes < 1024 ? $"{bytes} B" : bytes < 1024 * 1024 ? $"{bytes / 1024d:0.#} KB" : $"{bytes / 1024d / 1024d:0.#} MB";
	private static string ValoreOrDash(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value;
	private static string? PulisciValore(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
	private static long? ParseLong(string? value) => long.TryParse(value, out var parsed) ? parsed : null;

	private string GetTipoSegnalazioneDescrizione() => IsRichiesta ? "Richiesta" : IsReclamo ? "Reclamo" : "-";

	// Nel riepilogo mostra le descrizioni dei lookup anziché gli identificativi o i codici selezionati.
	private static string GetDescrizioneLong(IReadOnlyList<LookupLongDto> items, string? idValue)
	{
		if (!long.TryParse(idValue, out var id))
			return "-";

		return items.FirstOrDefault(x => x.Id == id)?.Descrizione ?? "-";
	}

	private static string GetDescrizioneString(IReadOnlyList<LookupStringDto> items, string? code)
		=> string.IsNullOrWhiteSpace(code) ? "-" : items.FirstOrDefault(x => x.Codice == code)?.Descrizione ?? "-";

	// Abbandona la pagina senza chiamare il servizio di creazione.
	private void Annulla() => NavigationManager.NavigateTo("/");

	// Gli attributi verificano lunghezze e formati; IValidatableObject aggiunge obblighi e regole tra campi.
	private sealed class NuovaSegnalazioneModel : IValidatableObject
	{
		public string? TipoSegnalazione { get; set; }
		public string? IdTipoRichiesta { get; set; }
		public string? Sorgente { get; set; }

		[MaxLength(200, ErrorMessage = "Il cognome non può superare 200 caratteri.")]
		public string? Cognome { get; set; }

		[MaxLength(200, ErrorMessage = "Il nome non può superare 200 caratteri.")]
		public string? Nome { get; set; }

		[MaxLength(200, ErrorMessage = "L'email non può superare 200 caratteri.")]
		[EmailAddress(ErrorMessage = "Inserire un indirizzo email valido.")]
		public string? Email { get; set; }

		[MaxLength(200, ErrorMessage = "La PEC non può superare 200 caratteri.")]
		[EmailAddress(ErrorMessage = "Inserire un indirizzo PEC valido.")]
		public string? Pec { get; set; }

		[MaxLength(200, ErrorMessage = "Il telefono non può superare 200 caratteri.")]
		public string? Telefono { get; set; }

		[MaxLength(200, ErrorMessage = "La ragione sociale non può superare 200 caratteri.")]
		public string? RagioneSociale { get; set; }

		public string? IdTipoAutoreRichiesta { get; set; }
		public string? IdTipoTematica { get; set; }
		public string? IdTipoLuogo { get; set; }

		[MaxLength(200, ErrorMessage = "Il comune specifico non può superare 200 caratteri.")]
		public string? ComuneLuogo { get; set; }

		[MaxLength(500, ErrorMessage = "L'indirizzo non può superare 500 caratteri.")]
		public string? Indirizzo { get; set; }

		[MaxLength(1000, ErrorMessage = "Il testo non può superare 1000 caratteri.")]
		public string? Testo { get; set; }

		public string? CodiceStruttura { get; set; }
		public string? DescrizioneLuogoSelezionata { get; set; }

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			// Senza un tipo selezionato evita di produrre anche gli errori dei campi non ancora visualizzati.
			if (string.IsNullOrWhiteSpace(TipoSegnalazione))
			{
				yield return new ValidationResult("Il tipo segnalazione è obbligatorio.", [nameof(TipoSegnalazione)]);
				yield break;
			}

			// Richieste e reclami hanno obblighi distinti; il comune è richiesto solo per il luogo specifico.
			if (TipoSegnalazione == "richiesta")
			{
				if (!long.TryParse(IdTipoRichiesta, out _))
					yield return new ValidationResult("Il tipo richiesta è obbligatorio.", [nameof(IdTipoRichiesta)]);
				if (!long.TryParse(IdTipoTematica, out _))
					yield return new ValidationResult("La tematica è obbligatoria.", [nameof(IdTipoTematica)]);
				if (!long.TryParse(IdTipoLuogo, out _))
					yield return new ValidationResult("Il luogo è obbligatorio.", [nameof(IdTipoLuogo)]);
				if (string.Equals(DescrizioneLuogoSelezionata, "Comune specifico", StringComparison.Ordinal) && string.IsNullOrWhiteSpace(ComuneLuogo))
					yield return new ValidationResult("Il comune specifico è obbligatorio.", [nameof(ComuneLuogo)]);
			}
			else if (TipoSegnalazione == "reclamo")
			{
				if (string.IsNullOrWhiteSpace(CodiceStruttura))
					yield return new ValidationResult("La struttura è obbligatoria.", [nameof(CodiceStruttura)]);
			}
			else
			{
				yield return new ValidationResult("Il tipo segnalazione non è valido.", [nameof(TipoSegnalazione)]);
			}

			// Regole comuni ai due tipi; i nomi dei membri associano ogni messaggio al relativo campo del form.
			if (string.IsNullOrWhiteSpace(Sorgente))
				yield return new ValidationResult("La sorgente è obbligatoria.", [nameof(Sorgente)]);
			if (string.IsNullOrWhiteSpace(Cognome))
				yield return new ValidationResult("Il cognome è obbligatorio.", [nameof(Cognome)]);
			if (string.IsNullOrWhiteSpace(Nome))
				yield return new ValidationResult("Il nome è obbligatorio.", [nameof(Nome)]);
			if (string.IsNullOrWhiteSpace(Email))
				yield return new ValidationResult("L'email è obbligatoria.", [nameof(Email)]);
			if (!long.TryParse(IdTipoAutoreRichiesta, out _))
				yield return new ValidationResult("La categoria è obbligatoria.", [nameof(IdTipoAutoreRichiesta)]);
			if (string.IsNullOrWhiteSpace(Testo))
				yield return new ValidationResult(TipoSegnalazione == "reclamo" ? "Il testo reclamo è obbligatorio." : "Il testo è obbligatorio.", [nameof(Testo)]);
		}
	}

	// L'identificativo temporaneo permette la rimozione dalla UI; i byte saranno inviati al servizio al salvataggio.
	private sealed record AllegatoNuovoStaging(Guid Id, string NomeFile, long Dimensione, byte[] Contenuto);
}