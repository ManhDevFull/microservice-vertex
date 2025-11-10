using be_dotnet_ecommerce1.Controllers;
using be_dotnet_ecommerce1.Data;
using be_dotnet_ecommerce1.Dtos;
using dotnet.Model;
using Microsoft.EntityFrameworkCore;

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
        public async Task<List<Variant>> GetVariantByFilter(FilterDTO dTO) // done
        {
            var sql = "select * from variant";
            var conditions = new List<string>();
            if (dTO.Filter != null)
            {
                foreach (var item in dTO.Filter)
                {
                    var key = item.Key;
                    var value = item.Value;
                    if (value != null)
                    {
                        var values = string.Join(",", value.Select(v => $"'{v}'"));
                        conditions.Add($"valuevariant ->> '{key}' IN ({values})");
                    }
                }
            }
            if (conditions.Count > 0)
            {
                sql += " where " + string.Join(" AND ", conditions);
                Console.Write(sql);
            }
            var result = await _connect.variants.FromSqlRaw(sql).ToListAsync();
            return result;
        }

    }
}