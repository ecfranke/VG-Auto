using System.Data;
using System.Globalization;
using VgAuto.Core.Application.Database;
using VgAuto.Core.Domain;

namespace DbUp.Scripts
{
    /// <summary>
    /// Names the estimates and invoices issued before document codes existed like their work:
    /// an invoice RP_TF_2019_HC_2026_09_28_15, an estimate OF_TF_2019_HC_2026_09_28_15 (a later offer of the same work …_15-1).
    /// The code is made from the current client and vehicle of the work, as the work code is.
    /// </summary>
    internal class Script0014_DocumentCodes : DbUp.Engine.IScript
    {
        private record Document(object Id, bool IsInvoice, int OfferNr, int WorkNumber, DateTime StartedOn, string? ClientName, int? VehicleYear, string? Manufacturer, string? Model);

        // the work of the document with its client and vehicle, as in the work list
        private const string Columns = "pr.id, w.number, w.startedon, concat_ws(' ', p.firstname, p.lastname, l.name) as clientname, v.year, v.producer, v.model";
        private const string Client = @"
            left join domain.legalclient l on l.id = w.clientid
            left join domain.privateclient p on p.id = w.clientid
            left join domain.vehicle v on v.id = w.vehicleid";

        public string ProvideScript(Func<IDbCommand> dbCommandFactory)
        {
            var d = SqlDialect.Current;
            var documents = new List<Document>();
            Read(dbCommandFactory, documents, true, d.Sql($@"select {Columns}, 0 as ordernr from domain.pricing pr
                inner join domain.work w on w.invoiceid = pr.id {Client}
                where pr.code is null"));
            Read(dbCommandFactory, documents, false, d.Sql($@"select {Columns}, o.ordernr from domain.pricing pr
                inner join domain.offer o on o.estimateid = pr.id
                inner join domain.work w on w.id = o.workid {Client}
                where pr.code is null"));

            foreach (var document in documents)
            {
                var number = document.WorkNumber.ToString(CultureInfo.InvariantCulture)
                    + (document.IsInvoice || document.OfferNr == 0 ? "" : "-" + document.OfferNr.ToString(CultureInfo.InvariantCulture));
                var code = WorkCode.Format(document.IsInvoice, document.ClientName, document.VehicleYear, document.Manufacturer, document.Model, document.StartedOn, number);
                using var update = dbCommandFactory();
                update.CommandText = d.Sql("UPDATE domain.pricing SET code = @Code WHERE id = @Id");
                AddParameter(update, "@Code", code);
                AddParameter(update, "@Id", document.Id);
                update.ExecuteNonQuery();
            }
            if (documents.Count > 0) Console.WriteLine($" {documents.Count} estimates and invoices named like their work.");
            return "";
        }

        private static void Read(Func<IDbCommand> dbCommandFactory, List<Document> documents, bool isInvoice, string sql)
        {
            using var command = dbCommandFactory();
            command.CommandText = sql;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                documents.Add(new Document(
                    reader.GetValue(0),
                    isInvoice,
                    Convert.ToInt32(reader.GetValue(7), CultureInfo.InvariantCulture),
                    Convert.ToInt32(reader.GetValue(1), CultureInfo.InvariantCulture),
                    reader.GetDateTime(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3),
                    reader.IsDBNull(4) ? null : Convert.ToInt32(reader.GetValue(4), CultureInfo.InvariantCulture),
                    reader.IsDBNull(5) ? null : reader.GetString(5),
                    reader.IsDBNull(6) ? null : reader.GetString(6)));
            }
        }

        private static void AddParameter(IDbCommand command, string name, object value)
        {
            var p = command.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            command.Parameters.Add(p);
        }
    }
}
