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

using Microsoft.EntityFrameworkCore;
using QuestionariURP.Domain.Entities;
using System.Reflection;

namespace QuestionariURP.Infrastructure.Persistence;

/// <summary>
/// DbContext principale dell'applicazione.
///
/// Entità in sola lettura (TipoQuestionario, Domanda, OpzioneRisposta):
/// esposte come IQueryable con AsNoTracking per impedire scritture accidentali
/// dal livello Application. Include() funziona correttamente su IQueryable&lt;T&gt;
/// tramite le estensioni EF Core.
///
/// Entità scrivibili:
/// - Token: il livello Application aggiorna esclusivamente DataUtilizzo.
/// - Compilazione, RispostaCompilata: scritte al termine della compilazione (F003).
/// </summary>
public class QuestionariDbContext : DbContext
{
	public QuestionariDbContext(DbContextOptions<QuestionariDbContext> options)
		: base(options) { }

	// ── Entità in sola lettura ─────────────────────────────────────────────

	public IQueryable<TipoQuestionario> TipiQuestionario =>
		Set<TipoQuestionario>().AsNoTracking();

	public IQueryable<Domanda> Domande =>
		Set<Domanda>().AsNoTracking();

	public IQueryable<OpzioneRisposta> OpzioniRisposta =>
		Set<OpzioneRisposta>().AsNoTracking();

	// ── Entità scrivibili ──────────────────────────────────────────────────

	public DbSet<Token> Token { get; set; } = null!;
	public DbSet<Compilazione> Compilazioni { get; set; } = null!;
	public DbSet<RispostaCompilata> RisposteCompilate { get; set; } = null!;

	// ── Configurazione del modello ─────────────────────────────────────────

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
	}
}
