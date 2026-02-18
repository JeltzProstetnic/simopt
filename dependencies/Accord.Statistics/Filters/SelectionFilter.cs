// Accord Statistics Library
// Accord.NET framework
// http://www.crsouza.com
//
// Copyright © César Souza, 2009
// cesarsouza at gmail.com
//

namespace Accord.Statistics.Filters
{
    using System.Data;

    public class SelectionFilter : IFilter
    {
        public string Expression { get; set; }
        public string OrderBy { get; set; }

        public SelectionFilter(string expression, string orderby)
        {
            this.Expression = expression;
            this.OrderBy = orderby;
        }

        public SelectionFilter()
            : this(string.Empty, string.Empty)
        {
        }

        public DataTable Apply(DataTable data)
        {
            DataTable table = data.Clone();

            var rows = data.Select(Expression, OrderBy);
            foreach (DataRow row in rows)
                table.ImportRow(row);

            return table;
        }

    }
}
