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

using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.FluentUI.AspNetCore.Components;
using NLog;
using NLog.Web;
using QuestionariURP;
using QuestionariURP.Application.Services;
using QuestionariURP.Application.State;
using QuestionariURP.Infrastructure.Persistence;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

var cartellaTemporanea = builder.Configuration["CartellaTemporanea"];
Directory.CreateDirectory(cartellaTemporanea);

builder.Logging.ClearProviders();
builder.Host.UseNLog();

builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

builder.Services.AddFluentUIComponents();

var logger = LogManager.Setup()
					   .LoadConfigurationFromAppSettings()
					   .GetCurrentClassLogger();

// DbContext Scoped: in Blazor Server ogni circuito ha il proprio scope,
// garantendo l'isolamento dello stato tra tab diverse (vedi project-instructions.md).
builder.Services.AddDbContext<QuestionariDbContext>(options =>
	options.UseOracle(builder.Configuration.GetConnectionString("Oracle")));

// Servizi application Scoped: mantengono lo stato della compilazione per circuito
builder.Services.AddScoped<QuestionarioAccessResolver>();
builder.Services.AddScoped<QuestionarioCompilazioneState>();
builder.Services.AddScoped<InvioRisposteService>();

// Salvataggio chiavi di protezione in file system
builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(
        new DirectoryInfo(cartellaTemporanea))
    .SetApplicationName(builder.Environment.ApplicationName);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

// Versione applicazione
Assembly assembly = Assembly.GetEntryAssembly()!;
AssemblyInformationalVersionAttribute versionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!;
logger.Info($"Avvio applicazione - V.{versionAttribute.InformationalVersion.Split('+')[0]}");


app.Run();
