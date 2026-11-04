using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TeaPremium.Domain.Catalog;
using TeaPremium.Domain.Commerce;
using TeaPremium.Domain.Common;
using TeaPremium.Domain.Content;
using TeaPremium.Domain.Leads;
using TeaPremium.Domain.Media;
using TeaPremium.Infrastructure.Persistence;

namespace TeaPremium.Api.Seeding;

// Fixed, obviously synthetic fixtures; never use as production catalog, orders or customer data.
internal static class DemoShowcaseSeeder
{
    private sealed record ProductFixture(
        string Slug, string Name, string Category, string Type, string Material,
        int? Capacity, string Summary, string Detail, string Picture, long ImageSize,
        string? ExtraPicture = null, long ExtraSize = 0);

    private static readonly ProductFixture[] ProductFixtures =
    [
        new("demo-am-tu-sa-tay-thi", "[DEMO] Ấm dáng Tây Thi", "demo-am-tu-sa",
            "Teapot", "Đất nung màu đỏ (minh họa)", 180,
            "Ấm trà dáng tròn, được dựng mẫu để kiểm tra cách hiển thị thông số và hình ảnh.",
            "Dáng ấm tròn mềm, miệng rộng và quai cong. Thông tin chỉ phục vụ kiểm thử giao diện, không xác nhận chất liệu hay xuất xứ thực tế.",
            "am-tu-sa-tay-thi", 2444, "am-tu-sa-tay-thi-chi-tiet", 2450),
        new("demo-am-tu-sa-thach-bieu", "[DEMO] Ấm dáng Thạch Biều", "demo-am-tu-sa",
            "Teapot", "Gốm màu nâu (minh họa)", 220,
            "Mẫu hình ảnh ấm trà màu nâu dùng để thử nhiều tông màu trong danh mục.",
            "Chất liệu và dung tích trong bản ghi này là dữ liệu giả lập, không phải chứng nhận của một sản phẩm có bán.",
            "am-tu-sa-thach-bieu", 2445),
        new("demo-am-gang-co-dien", "[DEMO] Ấm gang cổ điển", "demo-am-gang",
            "Kettle", "Gang đúc (minh họa)", 600,
            "Hình minh họa ấm gang với đường nét trầm, dùng để kiểm thử trang sản phẩm.",
            "Đây là sản phẩm giả lập. Không áp dụng nội dung về lớp men, an toàn thực phẩm hoặc nguồn gốc cho hàng thật.",
            "am-gang-co-dien", 2476, "am-gang-co-dien-chi-tiet", 2481),
        new("demo-am-thuy-tinh", "[DEMO] Ấm thủy tinh trong", "demo-tra-cu-thuy-tinh",
            "Teapot", "Thủy tinh (minh họa)", 450,
            "Mẫu trà cụ trong suốt phục vụ thử nghiệm ảnh trên nền kem và trang tìm kiếm.",
            "Hình sản phẩm được dựng riêng để thử giao diện. Không đại diện cho độ bền nhiệt hoặc thông số thương mại.",
            "am-thuy-tinh", 2440),
        new("demo-bo-tra-su-men-nga", "[DEMO] Bộ trà men ngà", "demo-bo-tra-va-chen",
            "TeaSet", "Gốm men ngà (minh họa)", 380,
            "Bộ trà phong cách tối giản, thử nghiệm cách trình bày sản phẩm theo bộ.",
            "Không có tồn kho, thương hiệu hay xuất xứ thực tế. Ảnh chỉ dùng cho Development.",
            "bo-tra-su-men-nga", 2564),
        new("demo-chen-tra-thien-muc", "[DEMO] Cặp chén trà tối màu", "demo-bo-tra-va-chen",
            "TeaCup", "Gốm màu tối (minh họa)", 70,
            "Cặp chén trà giúp thử cách hiển thị phụ kiện và hình ảnh có nhiều vật thể.",
            "Đây là ảnh minh họa kỹ thuật, không phải ảnh chụp sản phẩm để bán.",
            "chen-tra-thien-muc", 2491),
        new("demo-khay-tra-go", "[DEMO] Khay trà vân gỗ", "demo-khay-tra",
            "TeaTray", "Gỗ (minh họa)", null,
            "Khay trà mô phỏng với tông gỗ ấm, dành cho layout danh mục và bài viết.",
            "Kích thước, độ bền và vật liệu của hàng hóa thật chưa được cung cấp.",
            "khay-tra-go", 2146),
    ];

    private static readonly (string Slug, string Name)[] Categories =
    [
        ("demo-am-tu-sa", "Ấm tử sa · Demo"),
        ("demo-am-gang", "Ấm gang · Demo"),
        ("demo-tra-cu-thuy-tinh", "Trà cụ thủy tinh · Demo"),
        ("demo-bo-tra-va-chen", "Bộ trà & chén · Demo"),
        ("demo-khay-tra", "Khay trà · Demo"),
    ];

    private static readonly (string Name, string Phone, string Origin, string Source,
        bool Prior, string[] Tags)[] Customers =
    [
        ("[DEMO] Khách sỉ 01", "0900001001", "legacy_import", "legacy_base", true, ["wholesale", "vip"]),
        ("[DEMO] Khách dùng 02", "0900001002", "new", "google_organic", false, ["end_user"]),
        ("[DEMO] Khách Zalo 03", "0900001003", "new", "zalo_oa", false, ["end_user"]),
        ("[DEMO] Khách giới thiệu 04", "0900001004", "new", "referral", false, ["end_user"]),
        ("[DEMO] Khách sỉ 05", "0900001005", "legacy_import", "legacy_base", true, ["wholesale"]),
        ("[DEMO] Khách Facebook 06", "0900001006", "new", "facebook", false, ["end_user"]),
        ("[DEMO] Khách cũ chưa rõ 07", "0900001007", "legacy_import", "legacy_base", false, ["end_user"]),
        ("[DEMO] Khách trực tiếp 08", "0900001008", "new", "direct", false, ["end_user"]),
    ];

    private sealed record OrderFixture(int CustomerIndex, int ProductIndex, int Quantity,
        decimal Price, decimal? Cost, string Channel, bool? Returning, string State, string Campaign);

    private static readonly OrderFixture[] Orders =
    [
        new(0, 0, 2, 1850000m, 1050000m, "zalo_personal", true, "Completed", "demo-khach-cu"),
        new(1, 1, 1, 1450000m, 800000m, "website", false, "Completed", "demo-seo"),
        new(2, 2, 1, 2100000m, 1250000m, "zalo_oa", false, "Completed", "demo-oa"),
        new(3, 3, 2, 950000m, 520000m, "phone", false, "Completed", "demo-gioi-thieu"),
        new(4, 4, 3, 1380000m, 700000m, "zalo_personal", true, "Completed", "demo-khach-cu"),
        new(5, 5, 2, 380000m, null, "website", false, "Completed", "demo-facebook"),
        new(6, 6, 1, 880000m, 390000m, "zalo_oa", null, "Completed", "demo-oa"),
        new(7, 0, 1, 1850000m, 1050000m, "in_person", false, "Confirmed", "demo-cua-hang"),
        new(2, 1, 1, 1450000m, 800000m, "zalo_oa", true, "Cancelled", "demo-oa"),
    ];

    public static async Task<string> SeedAsync(AppDbContext db, Guid authorId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(db);
        if (authorId == Guid.Empty) throw new ArgumentException("Admin author required.", nameof(authorId));
        if ((await db.Database.GetPendingMigrationsAsync(ct)).Any())
            throw new InvalidOperationException("Apply Development migrations before seed-demo-showcase.");

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var brand = await db.Brands.SingleOrDefaultAsync(x => x.Slug == "teapremium-demo", ct);
        if (brand is null)
        {
            brand = new Brand("TeaPremium · Dữ liệu mẫu", "teapremium-demo");
            db.Brands.Add(brand);
        }

        var cats = new Dictionary<string, Category>(StringComparer.Ordinal);
        foreach (var (slug, name) in Categories)
        {
            var cat = await db.Categories.SingleOrDefaultAsync(x => x.Slug == slug, ct);
            if (cat is null)
            {
                cat = new Category(slug, name, "<p>Danh mục minh họa để kiểm tra giao diện TeaPremium. Không phải hàng hóa thật.</p>",
                    new SeoMetadata(name, "Danh mục dữ liệu mẫu dành riêng cho môi trường Development."));
                cat.MoveToInReview();
                cat.Publish();
                db.Categories.Add(cat);
            }
            cats[slug] = cat;
        }

        var assets = new Dictionary<string, MediaAsset>(StringComparer.Ordinal);
        foreach (var item in ProductFixtures)
        {
            await EnsureAssetAsync(item.Picture, item.ImageSize);
            if (item.ExtraPicture is { } extra) await EnsureAssetAsync(extra, item.ExtraSize);
        }

        var products = new List<Product>(ProductFixtures.Length);
        foreach (var fixture in ProductFixtures)
        {
            var product = await db.Products.SingleOrDefaultAsync(x => x.Slug == fixture.Slug, ct);
            if (product is null)
            {
                var image = assets[fixture.Picture];
                product = new Product(fixture.Slug, fixture.Name, fixture.Summary,
                    "{}", $"<h2>Minh họa sản phẩm</h2><p>{fixture.Detail}</p><p>Ảnh và thông số chỉ dùng để thử UI, không phải thông tin hàng thương mại.</p>",
                    fixture.Type, JsonSerializer.Serialize(new { model = "demo", sample = true }),
                    new SeoMetadata(fixture.Name, fixture.Summary, ogImageAssetId: image.Id),
                    sku: "DEMO-" + (products.Count + 1).ToString("00", System.Globalization.CultureInfo.InvariantCulture),
                    primaryCategoryId: cats[fixture.Category].Id, brandId: brand.Id,
                    materialText: fixture.Material, capacityMl: fixture.Capacity);
                product.ProductCategories.Add(new ProductCategory(product.Id, cats[fixture.Category].Id));
                product.Media.Add(new ProductMedia(product.Id, image.Id,
                    "Minh họa demo: " + fixture.Name, 0, true));
                if (fixture.ExtraPicture is { } extra)
                    product.Media.Add(new ProductMedia(product.Id, assets[extra].Id,
                        "Góc nhìn minh họa thứ hai: " + fixture.Name, 1, false));
                product.MoveToInReview();
                product.Publish(DateTimeOffset.UtcNow);
                db.Products.Add(product);
            }
            products.Add(product);
        }
        await db.SaveChangesAsync(ct);

        var knowledge = await db.ArticleCategories.SingleOrDefaultAsync(x => x.Slug == "demo-kien-thuc-tra-cu", ct);
        if (knowledge is null)
        {
            knowledge = new ArticleCategory("demo-kien-thuc-tra-cu", "Kiến thức trà cụ · Demo");
            db.ArticleCategories.Add(knowledge);
        }
        var examples = new[]
        {
            (Slug: "demo-chon-dung-tich-am-tra", Title: "[DEMO] Chọn dung tích ấm theo thói quen",
                Excerpt: "Bài mẫu gợi ý cách suy nghĩ về dung tích ấm khi sử dụng hằng ngày.",
                Content: "Hãy bắt đầu từ số người uống, cách pha và lượng trà thường dùng. Các thông số ví dụ trong bài chỉ nhằm thử giao diện.",
                Picture: "am-tu-sa-tay-thi"),
            (Slug: "demo-cham-soc-am-gang", Title: "[DEMO] Những điều nên hỏi khi chọn ấm gang",
                Excerpt: "Câu hỏi mẫu về lớp lót, cách sử dụng và hướng dẫn vệ sinh ấm gang.",
                Content: "Cần xác minh hướng dẫn sử dụng và vật liệu của mẫu ấm cụ thể từ nhà cung cấp trước khi tư vấn hoặc đăng bán.",
                Picture: "am-gang-co-dien"),
            (Slug: "demo-bay-tri-goc-tra", Title: "[DEMO] Bày một góc trà thật tĩnh",
                Excerpt: "Câu chuyện mẫu về ánh sáng, khoảng trống và cách sắp xếp trà cụ.",
                Content: "Một khay trà, chiếc ấm và vài chén có thể tạo cảm giác gọn gàng khi được đặt trong không gian vừa đủ.",
                Picture: "khay-tra-go"),
        };
        foreach (var example in examples)
        {
            if (await db.Articles.AnyAsync(x => x.Slug == example.Slug, ct)) continue;
            var contentJson = JsonSerializer.Serialize(new
            {
                type = "doc",
                content = new object[]
                {
                    new { type = "heading", attrs = new { level = 2 }, content = new[] { new { type = "text", text = example.Title } } },
                    new { type = "paragraph", content = new[] { new { type = "text", text = example.Content } } },
                },
            });
            var article = new Article(example.Slug, example.Title, example.Excerpt,
                contentJson, $"<h2>{example.Title}</h2><p>{example.Content}</p>",
                authorId, assets[example.Picture].Id,
                new SeoMetadata(example.Title, example.Excerpt, ogImageAssetId: assets[example.Picture].Id));
            article.Categories.Add(new ArticleCategoryLink(article.Id, knowledge.Id));
            article.SubmitForReview();
            article.Publish(DateTimeOffset.UtcNow);
            db.Articles.Add(article);
        }
        await db.SaveChangesAsync(ct);

        var customers = new List<Customer>(Customers.Length);
        for (var index = 0; index < Customers.Length; index++)
        {
            var fixture = Customers[index];
            var normalized = "+84" + fixture.Phone[1..];
            var customer = await db.Customers.SingleOrDefaultAsync(x => x.NormalizedPhone == normalized, ct);
            if (customer is null)
            {
                customer = new Customer(fixture.Name, fixture.Phone, normalized, fixture.Origin,
                    fixture.Source, fixture.Prior, fixture.Tags);
                db.Customers.Add(customer);
            }
            customers.Add(customer);
        }
        await db.SaveChangesAsync(ct);
        for (var index = 0; index < customers.Count; index++)
        {
            var customer = customers[index];
            var notes = $"[DEMO] Lần tư vấn mẫu số {index + 1}. Khách quan tâm thông tin trà cụ; không phải trao đổi thực tế.";
            if (!await db.CustomerInteractions.AnyAsync(x => x.CustomerId == customer.Id && x.Notes == notes, ct))
                db.CustomerInteractions.Add(new CustomerInteraction(customer.Id,
                    index % 2 == 0 ? "zalo_personal" : "zalo_oa", "note", notes));
        }

        for (var index = 0; index < Orders.Length; index++)
        {
            var item = Orders[index];
            var requestId = new Guid($"00000000-0000-4000-8000-{(index + 1).ToString("D12", System.Globalization.CultureInfo.InvariantCulture)}");
            if (await db.SalesOrders.AnyAsync(x => x.RequestId == requestId, ct)) continue;
            var product = products[item.ProductIndex];
            var customer = customers[item.CustomerIndex];
            var order = new SalesOrder(customer.Id, requestId, null, item.Channel,
                customer.AcquisitionSource, item.Returning, item.Campaign,
                [new SalesOrderItem(product.Id, product.Name, product.Sku, item.Quantity, item.Price, item.Cost)]);
            if (item.State == "Completed") order.Transition(SalesOrderStatus.Completed);
            if (item.State == "Cancelled") order.Transition(SalesOrderStatus.Cancelled);
            db.SalesOrders.Add(order);
        }

        for (var index = 0; index < 4; index++)
        {
            var product = products[index];
            var message = $"[DEMO] Yêu cầu tư vấn minh họa #{index + 1} cho sản phẩm {product.Slug}.";
            if (await db.Leads.AnyAsync(x => x.Message == message, ct)) continue;
            db.Leads.Add(new Lead($"[DEMO] Khách hỏi #{index + 1}", Customers[index].Phone,
                null, message, product.Id, $"/san-pham/{product.Slug}", "web_form",
                JsonSerializer.Serialize(new { source = index % 2 == 0 ? "zalo" : "google", medium = "demo" }),
                DateTimeOffset.UtcNow));
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return $"Demo loaded: {products.Count} products, {Categories.Length} categories, {examples.Length} articles, " +
               $"{customers.Count} customers, {Orders.Length} order fixtures and 4 leads. Existing demo rows kept.";

        async Task EnsureAssetAsync(string picture, long byteSize)
        {
            if (assets.ContainsKey(picture)) return;
            var key = $"demo-products/{picture}.svg";
            var asset = await db.MediaAssets.SingleOrDefaultAsync(x => x.StorageKey == key, ct);
            if (asset is null)
            {
                asset = new MediaAsset(key, "image/svg+xml", byteSize, 960, 1120);
                db.MediaAssets.Add(asset);
            }
            assets[picture] = asset;
        }
    }
}
