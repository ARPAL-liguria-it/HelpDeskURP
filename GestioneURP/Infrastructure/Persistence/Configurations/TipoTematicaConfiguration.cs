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

public class TipoTematicaConfiguration : IEntityTypeConfiguration<TipoTematica>
{
	public void Configure(EntityTypeBuilder<TipoTematica> e)
	{
		e.ToTable("TIPI_TEMATICA");
		e.HasKey(x => x.IdTipoTematica);
		e.Property(x => x.IdTipoTematica).HasColumnName("ID_TIPO_TEMATICA");
		e.Property(x => x.DescrizioneTipoTematica).HasColumnName("DESCRIZIONE_TIPO_TEMATICA").HasMaxLength(50);
		e.Property(x => x.Ordine).HasColumnName("ORDINE");
	}
}
