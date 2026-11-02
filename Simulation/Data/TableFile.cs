using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ErasOfWar.Simulation.Data
{
    /// <summary>
    /// A raw workbook export (game_data/tables_*.json): sheet name -> table title -> rows.
    /// Each row is a column-name -> value object. "_notes" entries are free text, not tables.
    /// Typed models (e.g. <see cref="Base.BuildingCatalog"/>) are built from these.
    /// </summary>
    public sealed class TableFile
    {
        private readonly JObject _root;

        public string FileName { get; }

        public TableFile(string fileName, JObject root)
        {
            FileName = fileName;
            _root = root;
        }

        public IEnumerable<string> SheetNames => _root.Properties().Select(p => p.Name);

        public bool HasSheet(string sheet) => _root[sheet] is JObject;

        /// <summary>All tables in a sheet as (title, rows), skipping "_notes".</summary>
        public IReadOnlyList<KeyValuePair<string, IReadOnlyList<JObject>>> Tables(string sheet)
        {
            if (!(_root[sheet] is JObject s))
                throw new GameDataException($"{FileName}: sheet '{sheet}' not found");

            return s.Properties()
                .Where(p => p.Value is JArray)
                .Select(p => new KeyValuePair<string, IReadOnlyList<JObject>>(
                    p.Name, p.Value.OfType<JObject>().ToList()))
                .ToList();
        }

        /// <summary>Rows of the first (usually only) table in a sheet.</summary>
        public IReadOnlyList<JObject> Rows(string sheet)
        {
            var tables = Tables(sheet);
            if (tables.Count == 0)
                throw new GameDataException($"{FileName}: sheet '{sheet}' has no table");
            return tables[0].Value;
        }

        /// <summary>Free-text notes of a sheet (column -> text), or empty.</summary>
        public IReadOnlyDictionary<string, string> Notes(string sheet)
        {
            var notes = (_root[sheet] as JObject)?["_notes"] as JObject;
            return notes == null
                ? new Dictionary<string, string>()
                : notes.Properties().ToDictionary(p => p.Name, p => (string)p.Value);
        }
    }
}
