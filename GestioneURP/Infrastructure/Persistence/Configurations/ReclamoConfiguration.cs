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

public class ReclamoConfiguration : IEntityTypeConfiguration<Reclamo>
{
	public void Configure(EntityTypeBuilder<Reclamo> e)
	{
		e.ToTable("RECLAMI");
		e.HasKey(x => x.IdReclamo);
		e.Property(x => x.IdReclamo).HasColumnName("ID_RECLAMO").ValueGeneratedOnAdd();
		e.Property(x => x.DataInserimento).HasColumnName("DATA_INSERIMENTO");
		e.Property(x => x.IdTipoRichiesta).HasColumnName("ID_TIPO_RICHIESTA");
		e.Property(x => x.IdTipoAutoreRichiesta).HasColumnName("ID_TIPO_AUTORE_RICHIESTA");
		e.Property(x => x.CognomeAutore).HasColumnName("COGNOME_AUTORE").HasMaxLength(200);
		e.Property(x => x.NomeAutore).HasColumnName("NOME_AUTORE").HasMaxLength(200);
		e.Property(x => x.PecAutore).HasColumnName("PEC_AUTORE").HasMaxLength(200);
		e.Property(x => x.EmailAutore).HasColumnName("EMAIL_AUTORE").HasMaxLength(200);
		e.Property(x => x.TelefonoAutore).HasColumnName("TELEFONO_AUTORE").HasMaxLength(200);
		e.Property(x => x.Testo).HasColumnName("TESTO").HasMaxLength(3000);
		e.Property(x => x.CodiceStruttura).HasColumnName("CODICE_STRUTTURA").HasMaxLength(20);
		e.Property(x => x.Codice).HasColumnName("CODICE").HasMaxLength(20);
		e.Property(x => x.StatoPratica).HasColumnName("STATO_PRATICA").HasMaxLength(64).IsConcurrencyToken();
		e.Property(x => x.DataPresaInCarico).HasColumnName("DATA_PRESA_IN_CARICO");
		e.Property(x => x.OperatorePresaInCarico).HasColumnName("OPERATORE_PRESA_IN_CARICO").HasMaxLength(200);
		e.Property(x => x.IdOperatorePresaInCarico).HasColumnName("ID_OPERATORE_PRESA_IN_CARICO");
		e.Property(x => x.AnnotazioniPresaInCarico).HasColumnName("ANNOTAZIONI_PRESA_IN_CARICO").HasMaxLength(4000);
		e.Property(x => x.DataAssegnazioneAltroOperatore).HasColumnName("DATA_ASSEGNAZIONE_ALTRO_OPERATORE");
		e.Property(x => x.OperatoreAssegnatario).HasColumnName("OPERATORE_ASSEGNATARIO").HasMaxLength(200);
		e.Property(x => x.IdOperatoreAssegnatario).HasColumnName("ID_OPERATORE_ASSEGNATARIO");
		e.Property(x => x.AnnotazioniAssegnazione).HasColumnName("ANNOTAZIONI_ASSEGNAZIONE").HasMaxLength(4000);
		e.Property(x => x.DataInoltroAltraStruttura).HasColumnName("DATA_INOLTRO_ALTRA_STRUTTURA");
		e.Property(x => x.StrutturaDestinataria).HasColumnName("STRUTTURA_DESTINATARIA").HasMaxLength(200);
		e.Property(x => x.AnnotazioniInoltro).HasColumnName("ANNOTAZIONI_INOLTRO").HasMaxLength(4000);
		e.Property(x => x.DataRispostaStruttura).HasColumnName("DATA_RISPOSTA_STRUTTURA");
		e.Property(x => x.RispostaDaStruttura).HasColumnName("RISPOSTA_DA_STRUTTURA").HasMaxLength(4000);
		e.Property(x => x.DataRisposta).HasColumnName("DATA_RISPOSTA");
		e.Property(x => x.TestoRisposta).HasColumnName("TESTO_RISPOSTA").HasMaxLength(4000);
		e.Property(x => x.AnnotazioniRisposta).HasColumnName("ANNOTAZIONI_RISPOSTA").HasMaxLength(4000);
		e.Property(x => x.DataChiusura).HasColumnName("DATA_CHIUSURA");
		e.Property(x => x.AnnotazioniChiusura).HasColumnName("ANNOTAZIONI_CHIUSURA").HasMaxLength(4000);
		e.Property(x => x.UltimoAggiornamento).HasColumnName("ULTIMO_AGGIORNAMENTO");
		e.Property(x => x.OperatoreUltimoAggiornamento).HasColumnName("OPERATORE_ULTIMO_AGGIORNAMENTO").HasMaxLength(100);
		e.Property(x => x.Sorgente).HasColumnName("TIPO_SORGENTE").HasMaxLength(20);
		e.Property(x => x.RagioneSociale).HasColumnName("RAGIONE_SOCIALE").HasMaxLength(200);
		e.Property(x => x.Eliminato).HasColumnName("ELIMINATO").HasDefaultValue("N");
		e.Property(x => x.CodiceStrutturaDestinataria).HasColumnName("CODICE_STRUTTURA_DESTINATARIA").HasMaxLength(20);
	}
}
