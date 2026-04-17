-- Demo master data for categories, brands, colors, sizes
-- Safe to re-run because of ON CONFLICT (slug) DO NOTHING

-- Categories
INSERT INTO categories (id, name, slug, parentid, image, isactive)
VALUES
    (uuid_generate_v4(), 'Electronics', 'electronics', NULL, NULL, TRUE),
    (uuid_generate_v4(), 'Fashion', 'fashion', NULL, NULL, TRUE),
    (uuid_generate_v4(), 'Home Appliances', 'home-appliances', NULL, NULL, TRUE),
    (uuid_generate_v4(), 'Mobiles', 'mobiles', (SELECT id FROM categories WHERE slug = 'electronics' LIMIT 1), NULL, TRUE),
    (uuid_generate_v4(), 'Laptops', 'laptops', (SELECT id FROM categories WHERE slug = 'electronics' LIMIT 1), NULL, TRUE),
    (uuid_generate_v4(), 'Men Clothing', 'men-clothing', (SELECT id FROM categories WHERE slug = 'fashion' LIMIT 1), NULL, TRUE),
    (uuid_generate_v4(), 'Women Clothing', 'women-clothing', (SELECT id FROM categories WHERE slug = 'fashion' LIMIT 1), NULL, TRUE)
ON CONFLICT (slug) DO NOTHING;

-- Brands
INSERT INTO brands (id, name, slug, description, logo, isactive, createdby, createddate, updateddate)
VALUES
    (uuid_generate_v4(), 'Samsung', 'samsung', 'Electronics and appliances brand', NULL, TRUE, NULL, CURRENT_TIMESTAMP, NULL),
    (uuid_generate_v4(), 'Apple', 'apple', 'Premium electronics brand', NULL, TRUE, NULL, CURRENT_TIMESTAMP, NULL),
    (uuid_generate_v4(), 'Nike', 'nike', 'Sports and fashion brand', NULL, TRUE, NULL, CURRENT_TIMESTAMP, NULL),
    (uuid_generate_v4(), 'Adidas', 'adidas', 'Sportswear and footwear brand', NULL, TRUE, NULL, CURRENT_TIMESTAMP, NULL),
    (uuid_generate_v4(), 'LG', 'lg', 'Consumer electronics and appliances', NULL, TRUE, NULL, CURRENT_TIMESTAMP, NULL)
ON CONFLICT (slug) DO NOTHING;

-- Colors
INSERT INTO colors (id, name, slug, hexcode, isactive, isdeleted, createddate)
VALUES
    (uuid_generate_v4(), 'Black', 'black', '#000000', TRUE, FALSE, CURRENT_TIMESTAMP),
    (uuid_generate_v4(), 'White', 'white', '#FFFFFF', TRUE, FALSE, CURRENT_TIMESTAMP),
    (uuid_generate_v4(), 'Blue', 'blue', '#0000FF', TRUE, FALSE, CURRENT_TIMESTAMP),
    (uuid_generate_v4(), 'Red', 'red', '#FF0000', TRUE, FALSE, CURRENT_TIMESTAMP),
    (uuid_generate_v4(), 'Green', 'green', '#00AA00', TRUE, FALSE, CURRENT_TIMESTAMP)
ON CONFLICT (slug) DO NOTHING;

-- Sizes
INSERT INTO sizes (id, name, slug, isactive, isdeleted, createddate, updateddate)
VALUES
    (uuid_generate_v4(), 'XS', 'xs', TRUE, FALSE, CURRENT_TIMESTAMP, NULL),
    (uuid_generate_v4(), 'S', 's', TRUE, FALSE, CURRENT_TIMESTAMP, NULL),
    (uuid_generate_v4(), 'M', 'm', TRUE, FALSE, CURRENT_TIMESTAMP, NULL),
    (uuid_generate_v4(), 'L', 'l', TRUE, FALSE, CURRENT_TIMESTAMP, NULL),
    (uuid_generate_v4(), 'XL', 'xl', TRUE, FALSE, CURRENT_TIMESTAMP, NULL),
    (uuid_generate_v4(), 'XXL', 'xxl', TRUE, FALSE, CURRENT_TIMESTAMP, NULL)
ON CONFLICT (slug) DO NOTHING;
