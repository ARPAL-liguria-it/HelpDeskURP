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
using System.Reflection;

namespace GestioneURP.Infrastructure.Persistence;

public class UrpHdDbContext(DbContextOptions<UrpHdDbContext> options) : DbContext(options)
{
	public DbSet<Richiesta> Richieste => Set<Richiesta>();
	public DbSet<Reclamo> Reclami => Set<Reclamo>();
	public DbSet<AllegatoRichiesta> AllegatiRichieste => Set<AllegatoRichiesta>();
	public DbSet<AllegatoReclamo> AllegatiReclami => Set<AllegatoReclamo>();
	public DbSet<RegistroAttivita> RegistroAttivita => Set<RegistroAttivita>();
	public DbSet<TipoRichiesta> TipiRichiesta => Set<TipoRichiesta>();
	public DbSet<TipoAutoreRichiesta> TipiAutoreRichiesta => Set<TipoAutoreRichiesta>();
	public DbSet<TipoTematica> TipiTematica => Set<TipoTematica>();
	public DbSet<TipoLuogo> TipiLuogo => Set<TipoLuogo>();
	public DbSet<TipoSorgente> TipiSorgente => Set<TipoSorgente>();
	public DbSet<TipoStruttura> TipiStruttura => Set<TipoStruttura>();
	public DbSet<DescrizioneComuneProvincia> ElencoComuni => Set<DescrizioneComuneProvincia>();
	public DbSet<QuestionarioSorgente> QuestionariSorgente => Set<QuestionarioSorgente>();
	public DbSet<QuestionarioTipo> QuestionariTipo => Set<QuestionarioTipo>();
	public DbSet<QuestionarioToken> QuestionariToken => Set<QuestionarioToken>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.HasDefaultSchema("URP_HD");
		modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

		// Filtro globale per eliminazione logica
		modelBuilder.Entity<Richiesta>()
			.HasQueryFilter(x => x.Eliminata != "S");

		modelBuilder.Entity<Reclamo>()
			.HasQueryFilter(x => x.Eliminato != "S");
	}
}
