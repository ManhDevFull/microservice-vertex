using be_dotnet_ecommerce1.Controllers;
using be_dotnet_ecommerce1.Data;
using be_dotnet_ecommerce1.Dtos;
using dotnet.Model;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text;

namespace be_dotnet_ecommerce1.Repository.IRepository
{
    public class VariantRepository : IVariantRepository
    {
        private readonly ConnectData _connect;
        public VariantRepository(ConnectData connect)
        {
            _connect = connect;
        }
        public async Task<List<VariantFilterDTO>> GetValueVariant(int id)
        {
            var data = await _connect.Database
        .SqlQueryRaw<VariantFilterDTO>(@"
    SELECT 
                key, 
                array_agg(DISTINCT value ORDER BY value) AS values
            FROM (
                -- Lấy các thuộc tính từ valuevariant
                SELECT 
                    kv.key::text AS key, 
                    kv.value::text AS value
                FROM category 
                JOIN product ON category.id = product.category
                JOIN variant ON product.id = variant.product_id
                CROSS JOIN LATERAL jsonb_each_text(variant.valuevariant) AS kv(key, value)
                WHERE category.id = {0} 
                AND variant.isdeleted = false
                AND product.isdeleted = false
                
                UNION ALL
                
                -- Thêm giá như một thuộc tính
                SELECT 
                    'price' AS key,
                    v.price::text AS value
                FROM category c
                JOIN product p ON c.id = p.category
                JOIN variant v ON p.id = v.product_id
                WHERE c.id = {0}
                AND v.isdeleted = false
                AND p.isdeleted = false
            ) AS combined
            GROUP BY key
            ORDER BY key;", id, id)
        .ToListAsync();

            return data;
        }
        public async Task<List<Variant>> GetVariantByFilter(FilterDTO dTO)
        {
            var sql = new StringBuilder(@"
SELECT v.*
FROM variant v
JOIN product p ON p.id = v.product_id
WHERE NOT v.isdeleted
  AND NOT p.isdeleted");
            var parameters = new List<NpgsqlParameter>();
            var paramIndex = 0;

            if (dTO.Filter != null)
            {
                foreach (var entry in dTO.Filter)
                {
                    var key = entry.Key?.Trim();
                    var values = entry.Value?
                        .Where(v => !string.IsNullOrWhiteSpace(v))
                        .ToArray();

                    if (string.IsNullOrWhiteSpace(key) || values == null || values.Length == 0)
                    {
                        continue;
                    }

                    if (key.Equals("price", StringComparison.OrdinalIgnoreCase))
                    {
                        var priceValues = values
                            .Select(v => int.TryParse(v.Trim(), out var parsed) ? parsed : (int?)null)
                            .Where(v => v.HasValue)
                            .Select(v => v!.Value)
                            .Distinct()
                            .ToArray();

                        if (priceValues.Length == 0)
                        {
                            continue;
                        }

                        var placeholders = new List<string>();
                        foreach (var price in priceValues)
                        {
                            var paramName = $"p{paramIndex++}";
                            parameters.Add(new NpgsqlParameter(paramName, price));
                            placeholders.Add($"@{paramName}");
                        }

                        sql.Append($" AND v.price IN ({string.Join(", ", placeholders)})");
                        continue;
                    }

                    var allowedValues = values
                        .Select(v => v.Trim())
                        .Where(v => v.Length > 0)
                        .Distinct()
                        .ToArray();

                    if (allowedValues.Length == 0)
                    {
                        continue;
                    }

                    var valuePlaceholders = new List<string>();
                    foreach (var value in allowedValues)
                    {
                        var paramName = $"p{paramIndex++}";
                        parameters.Add(new NpgsqlParameter(paramName, value));
                        valuePlaceholders.Add($"@{paramName}");
                    }

                    var escapedKey = key.Replace("'", "''");
                    sql.Append($" AND (v.valuevariant ->> '{escapedKey}') IN ({string.Join(", ", valuePlaceholders)})");
                }
            }

            var parameterArray = parameters.Count > 0
                ? parameters.Cast<object>().ToArray()
                : Array.Empty<object>();

            var query = parameters.Count > 0
                ? _connect.variants.FromSqlRaw(sql.ToString(), parameterArray)
                : _connect.variants.FromSqlRaw(sql.ToString());

            var result = await query
                .AsNoTracking()
                .ToListAsync();

            return result;
        }

    }
}
