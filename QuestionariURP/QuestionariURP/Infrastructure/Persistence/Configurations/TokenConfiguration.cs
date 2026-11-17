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
using QuestionariURP.Domain.Entities;

namespace QuestionariURP.Infrastructure.Persistence.Configurations;

internal class TokenConfiguration : IEntityTypeConfiguration<Token>
{
	public void Configure(EntityTypeBuilder<Token> builder)
	{
		builder.ToTable("QUESTIONARI_TOKEN", "URP_HD");

		builder.HasKey(t => t.Valore);
		builder.Property(t => t.Valore)
			.HasColumnName("TOKEN")
			.HasColumnType("VARCHAR2(100)")
			.IsRequired();

		builder.Property(t => t.TipoQuestionarioCodice)
			.HasColumnName("TIPO_QUESTIONARIO_CODICE")
			.HasColumnType("VARCHAR2(50)")
			.IsRequired();

		builder.Property(t => t.Sorgente)
			.HasColumnName("SORGENTE")
			.HasColumnType("VARCHAR2(100)")
			.IsRequired();

		builder.Property(t => t.DataUtilizzo)
			.HasColumnName("DATA_UTILIZZO")
			.HasColumnType("TIMESTAMP");

		// Proprietà calcolata: non mappata su colonna.
		builder.Ignore(t => t.EUtilizzabile);
	}
}
