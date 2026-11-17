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

public class TipoAutoreRichiestaConfiguration : IEntityTypeConfiguration<TipoAutoreRichiesta>
{
	public void Configure(EntityTypeBuilder<TipoAutoreRichiesta> e)
	{
		e.ToTable("TIPI_AUTORE_RICHIESTA");
		e.HasKey(x => x.IdTipoAutoreRichiesta);
		e.Property(x => x.IdTipoAutoreRichiesta).HasColumnName("ID_TIPO_AUTORE_RICHIESTA");
		e.Property(x => x.DescrizioneTipoAutoreRichiesta).HasColumnName("DESCRIZIONE_TIPO_AUTORE_RICHIESTA").HasMaxLength(50);
		e.Property(x => x.Ordine).HasColumnName("ORDINE");
	}
}
