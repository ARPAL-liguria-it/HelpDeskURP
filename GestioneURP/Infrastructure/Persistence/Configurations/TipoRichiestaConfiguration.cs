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
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestioneURP.Infrastructure.Persistence.Configurations;

public class TipoRichiestaConfiguration : IEntityTypeConfiguration<TipoRichiesta>
{
	public void Configure(EntityTypeBuilder<TipoRichiesta> e)
	{
		e.ToTable("TIPI_RICHIESTA");
		e.HasKey(x => x.IdTipoRichiesta);
		e.Property(x => x.IdTipoRichiesta).HasColumnName("ID_TIPO_RICHIESTA");
		e.Property(x => x.DescrizioneTipoRichiesta).HasColumnName("DESCRIZIONE_TIPO_RICHIESTA").HasMaxLength(100);
		e.Property(x => x.Dettaglio).HasColumnName("DETTAGLIO").HasMaxLength(200);
		e.Property(x => x.Ordine).HasColumnName("ORDINE");
	}
}
