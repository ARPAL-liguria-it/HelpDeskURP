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

using System;
using System.Collections.Generic;

namespace URP_HD.Data;

public partial class RICHIESTE
{
    public int ID_RICHIESTA { get; set; }

    public DateTime DATA_INSERIMENTO { get; set; }

    public int ID_TIPO_RICHIESTA { get; set; }

    public int ID_TIPO_AUTORE_RICHIESTA { get; set; }

    public string? COGNOME_AUTORE { get; set; }

    public string? NOME_AUTORE { get; set; }

    public string? PEC_AUTORE { get; set; }

    public string EMAIL_AUTORE { get; set; } = null!;

    public string? TELEFONO_AUTORE { get; set; }

    public int ID_TIPO_TEMATICA { get; set; }

    public int ID_TIPO_LUOGO { get; set; }

    public string? COMUNE_LUOGO { get; set; }

    public string TESTO { get; set; } = null!;

    public string? CODICE { get; set; }

    public string? TIPO_SORGENTE { get; set; }

    public string? INDIRIZZO { get; set; }

    public string? RAGIONE_SOCIALE { get; set; }

    public string? STATO_PRATICA { get; set; }

    public DateTime? DATA_PRESA_IN_CARICO { get; set; }

    public string? OPERATORE_PRESA_IN_CARICO { get; set; }

    public int? ID_OPERATORE_PRESA_IN_CARICO { get; set; }

    public string? ANNOTAZIONI_PRESA_IN_CARICO { get; set; }

    public DateTime? DATA_ASSEGNAZIONE_ALTRO_OPERATORE { get; set; }

    public string? OPERATORE_ASSEGNATARIO { get; set; }

    public int? ID_OPERATORE_ASSEGNATARIO { get; set; }

    public string? ANNOTAZIONI_ASSEGNAZIONE { get; set; }

    public DateTime? DATA_INOLTRO_ALTRA_STRUTTURA { get; set; }

    public string? STRUTTURA_DESTINATARIA { get; set; }

    public string? ANNOTAZIONI_INOLTRO { get; set; }

    public DateTime? DATA_RISPOSTA_STRUTTURA { get; set; }

    public string? RISPOSTA_DA_STRUTTURA { get; set; }

    public DateTime? DATA_RISPOSTA { get; set; }

    public string? TESTO_RISPOSTA { get; set; }

    public string? ANNOTAZIONI_RISPOSTA { get; set; }

    public DateTime? DATA_CHIUSURA { get; set; }

    public string? ANNOTAZIONI_CHIUSURA { get; set; }

    public DateTime? ULTIMO_AGGIORNAMENTO { get; set; }

    public string? OPERATORE_ULTIMO_AGGIORNAMENTO { get; set; }

    public string ELIMINATA { get; set; } = null!;

    public virtual ICollection<ALLEGATI_RICHIESTE> ALLEGATI_RICHIESTE { get; set; } = new List<ALLEGATI_RICHIESTE>();

    public virtual TIPI_AUTORE_RICHIESTA ID_TIPO_AUTORE_RICHIESTANavigation { get; set; } = null!;

    public virtual TIPI_LUOGO ID_TIPO_LUOGONavigation { get; set; } = null!;

    public virtual TIPI_RICHIESTA ID_TIPO_RICHIESTANavigation { get; set; } = null!;

    public virtual TIPI_TEMATICA ID_TIPO_TEMATICANavigation { get; set; } = null!;

    public virtual TIPI_SORGENTE? TIPO_SORGENTENavigation { get; set; }
}
