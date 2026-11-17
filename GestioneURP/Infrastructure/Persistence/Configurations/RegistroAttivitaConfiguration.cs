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

public class RegistroAttivitaConfiguration : IEntityTypeConfiguration<RegistroAttivita>
{
	public void Configure(EntityTypeBuilder<RegistroAttivita> e)
	{
		e.ToTable("REGISTRO_ATTIVITA");
		e.HasKey(x => x.IdLog);
		e.Property(x => x.IdLog).HasColumnName("ID_LOG").ValueGeneratedOnAdd();
		e.Property(x => x.TipoSegnalazione).HasColumnName("TIPO_SEGNALAZIONE").HasMaxLength(20).IsRequired();
		e.Property(x => x.IdSegnalazione).HasColumnName("ID_SEGNALAZIONE").IsRequired();
		e.Property(x => x.DataOraEvento).HasColumnName("DATA_ORA_EVENTO").IsRequired();
		e.Property(x => x.Operatore).HasColumnName("OPERATORE").HasMaxLength(200).IsRequired();
		e.Property(x => x.StatoPratica).HasColumnName("STATO_PRATICA").HasMaxLength(64).IsRequired();
		e.Property(x => x.Dettagli).HasColumnName("DETTAGLI").HasMaxLength(4000).IsUnicode(false);
	}
}
