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

public class QuestionarioTokenConfiguration : IEntityTypeConfiguration<QuestionarioToken>
{
	public void Configure(EntityTypeBuilder<QuestionarioToken> e)
	{
		e.ToTable("QUESTIONARI_TOKEN");
		e.HasKey(x => x.Token);
		e.Property(x => x.Token).HasColumnName("TOKEN").HasMaxLength(100).IsRequired();
		e.Property(x => x.TipoQuestionarioCodice).HasColumnName("TIPO_QUESTIONARIO_CODICE").HasMaxLength(50);
		e.Property(x => x.Sorgente).HasColumnName("SORGENTE").HasMaxLength(100);
		e.Property(x => x.DataUtilizzo).HasColumnName("DATA_UTILIZZO");
		e.Property(x => x.Email).HasColumnName("EMAIL").HasMaxLength(200);

		e.HasOne<QuestionarioTipo>()
			.WithMany()
			.HasForeignKey(x => x.TipoQuestionarioCodice)
			.HasConstraintName("FK_QUEST_TOKEN_TIPO");
	}
}
