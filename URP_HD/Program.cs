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

using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.FluentUI.AspNetCore.Components;
using NLog;
using NLog.Web;
using System.Reflection;
using URP_HD.Components;
using URP_HD.Data;
using Microsoft.AspNetCore.DataProtection;

var logger = LogManager.Setup()
					   .LoadConfigurationFromAppSettings()
					   .GetCurrentClassLogger();

var builder = WebApplication.CreateBuilder(args);

// Cartella temporanea per i file caricati
var cartellaTemporanea = builder.Configuration["CartellaTemporanea"];
Directory.CreateDirectory(cartellaTemporanea);

// Imposta la cartella temporanea
Environment.SetEnvironmentVariable("TEMP", cartellaTemporanea);
Environment.SetEnvironmentVariable("TMP", cartellaTemporanea);

// Add services to the container.
builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

builder.Services.AddFluentUIComponents();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<PuliziaFileTemporaneiService>();

// Sessione
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
	options.IdleTimeout = TimeSpan.FromMinutes(30);
	options.Cookie.HttpOnly = true;
	options.Cookie.IsEssential = true;
});

builder.Logging.ClearProviders();
builder.Host.UseNLog();

builder.Services.AddDbContextFactory<URP_HDContext>(options =>
	options.UseOracle(builder.Configuration["StringaConnessioneDB"]));

// Salvataggio chiavi di protezione in file system
builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(
        new DirectoryInfo(cartellaTemporanea))
    .SetApplicationName(builder.Environment.ApplicationName);

var app = builder.Build();

// Aggiunti i middleware per gestire gli header X-Forwarded-For, X-Forwarded-Proto e X-Forwarded-Host con l'utilizzo del reverse proxy
// e reset degli ip di rete accettati
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
	ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

// Sessione
app.UseSession();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

// Versione applicazione
Assembly assembly = Assembly.GetEntryAssembly()!;
AssemblyInformationalVersionAttribute versionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!;
logger.Info($"Avvio applicazione - V.{versionAttribute.InformationalVersion.Split('+')[0]}");

app.Run();
