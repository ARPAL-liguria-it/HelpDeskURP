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
using GestioneURP.Components;
using GestioneURP.Components.State;
using GestioneURP.Infrastructure;
using GestioneURP.Infrastructure.Email;
using GestioneURP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.FluentUI.AspNetCore.Components;
using NLog.Web;
using System.Globalization;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

var cartellaTemporanea = builder.Configuration["CartellaTemporanea"];
Directory.CreateDirectory(cartellaTemporanea);

builder.Logging.ClearProviders();
builder.Host.UseNLog();

// Add services to the container.
builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();
builder.Services.AddFluentUIComponents();
builder.Services.AddHttpClient();

builder.Services.AddScoped<SegnalazioniService>();
builder.Services.AddScoped<QuestionariService>();
builder.Services.AddScoped<InvioEmailService>();
builder.Services.AddScoped<SmtpEmailSender>();
builder.Services.AddScoped<EmailCircuitHandler>();
builder.Services.AddScoped<CircuitHandler>(services => services.GetRequiredService<EmailCircuitHandler>());
builder.Services.AddScoped<HomeFiltriState>();

builder.Services.AddDbContext<UrpHdDbContext>(options =>
	options.UseOracle(builder.Configuration.GetConnectionString("StringaConnessioneDB")));
builder.Services.AddLocalization();

builder.Services.AddExceptionHandler(options =>
{
	options.ExceptionHandlingPath = "/Error";
	options.CreateScopeForErrors = true;
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddAuthorization();

builder.Services.AddScoped<OperatoreState>();

// Salvataggio chiavi di protezione in file system
builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(
        new DirectoryInfo(cartellaTemporanea))
    .SetApplicationName(builder.Environment.ApplicationName);

var app = builder.Build();

var culture = new CultureInfo("it-IT");
var localizationOptions = new RequestLocalizationOptions
{
	DefaultRequestCulture = new RequestCulture(culture),
	SupportedCultures = new[] { culture },
	SupportedUICultures = new[] { culture }
};

app.UseRequestLocalization(localizationOptions);

var versione = Assembly.GetEntryAssembly()?
	.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
	.InformationalVersion
	?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
	?? "n/d";

app.Logger.LogInformation("Avvio applicazione - Versione {Versione}", versione);

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (!app.Environment.IsDevelopment())
{
	app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapGet("/segnalazioni/allegati/download/{tipo}/{idAllegato:long}", async (string tipo, long idAllegato, UrpHdDbContext db, IConfiguration configuration) =>
{
	var tipoNormalizzato = tipo.Trim().ToLowerInvariant();
	string? cartellaAllegati;
	string? nomeFile;

	if (tipoNormalizzato == "richiesta")
	{
		var allegato = await db.AllegatiRichieste.AsNoTracking()
			.FirstOrDefaultAsync(x => x.IdAllegato == idAllegato);
		if (allegato is null || string.IsNullOrWhiteSpace(allegato.NomeFile))
			return Results.NotFound();

		cartellaAllegati = configuration["CartellaAllegatiRichieste"];
		nomeFile = allegato.NomeFile;
	}
	else if (tipoNormalizzato == "reclamo")
	{
		var allegato = await db.AllegatiReclami.AsNoTracking()
			.FirstOrDefaultAsync(x => x.IdAllegato == idAllegato);
		if (allegato is null || string.IsNullOrWhiteSpace(allegato.NomeFile))
			return Results.NotFound();

		cartellaAllegati = configuration["CartellaAllegatiReclami"];
		nomeFile = allegato.NomeFile;
	}
	else
	{
		return Results.BadRequest("Tipo segnalazione non valido.");
	}

	if (string.IsNullOrWhiteSpace(cartellaAllegati))
		return Results.Problem("Cartella allegati non configurata.");

	var percorsoFile = Path.Combine(cartellaAllegati, idAllegato.ToString());
	if (!File.Exists(percorsoFile))
		return Results.NotFound();

	return Results.File(percorsoFile, "application/octet-stream", nomeFile);
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

app.Run();
