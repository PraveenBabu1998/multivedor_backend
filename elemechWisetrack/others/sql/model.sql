CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- NOTE:
-- "AspNetUsers" and other ASP.NET Identity tables are created by EF migrations.
-- This file creates all custom tables used in DataBaseLayer.

CREATE TABLE IF NOT EXISTS categories (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(150) NOT NULL,
    slug VARCHAR(180) NOT NULL UNIQUE,
    parentid UUID NULL REFERENCES categories(id) ON DELETE SET NULL,
    image TEXT NULL,
    isactive BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS brands (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(150) NOT NULL,
    slug VARCHAR(180) NOT NULL UNIQUE,
    description TEXT NULL,
    logo TEXT NULL,
    isactive BOOLEAN NOT NULL DEFAULT TRUE,
    createdby UUID NULL,
    createddate TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updateddate TIMESTAMP NULL
);

CREATE TABLE IF NOT EXISTS colors (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL,
    slug VARCHAR(120) NOT NULL UNIQUE,
    hexcode VARCHAR(20) NULL,
    
    isactive BOOLEAN NOT NULL DEFAULT TRUE,
    isdeleted BOOLEAN NOT NULL DEFAULT FALSE,
    createddate TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS sizes (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL,
    slug VARCHAR(120) NOT NULL UNIQUE,
    
    isactive BOOLEAN NOT NULL DEFAULT TRUE,
    isdeleted BOOLEAN NOT NULL DEFAULT FALSE,
    createddate TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updateddate TIMESTAMP NULL
);

CREATE TABLE IF NOT EXISTS products (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(200) NOT NULL,
    slug VARCHAR(220) NOT NULL UNIQUE,
    shortdescription TEXT NULL,
    description TEXT NULL,
    categoryid UUID NOT NULL REFERENCES categories(id),
    subcategoryid UUID NULL REFERENCES categories(id),
    brandid UUID NULL REFERENCES brands(id),
    colorid UUID NULL REFERENCES colors(id),
    sizeid UUID NULL REFERENCES sizes(id),
    price NUMERIC(12,2) NOT NULL,
    discountprice NUMERIC(12,2) NULL,
    costprice NUMERIC(12,2) NULL,
    taxpercentage NUMERIC(6,2) NULL,
    sku VARCHAR(80) NOT NULL UNIQUE,
    stockquantity INT NOT NULL DEFAULT 0,
    minstockquantity INT NULL,
    trackinventory BOOLEAN NOT NULL DEFAULT TRUE,
    mainimage TEXT NULL,
    galleryimages TEXT[] NULL,
    weight NUMERIC(10,2) NULL,
    length NUMERIC(10,2) NULL,
    width NUMERIC(10,2) NULL,
    height NUMERIC(10,2) NULL,
    metatitle VARCHAR(255) NULL,
    metadescription TEXT NULL,
    metakeywords TEXT NULL,
    isactive BOOLEAN NOT NULL DEFAULT TRUE,
    isfeatured BOOLEAN NOT NULL DEFAULT FALSE,
    isdeleted BOOLEAN NOT NULL DEFAULT FALSE,
    sales_status VARCHAR(20) NOT NULL DEFAULT 'Inactive',
    createdby UUID NULL,
    createddate TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);




CREATE TABLE IF NOT EXISTS carts (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NULL,
    guest_id UUID NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS cart_items (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    cart_id UUID NOT NULL REFERENCES carts(id) ON DELETE CASCADE,
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    quantity INT NOT NULL DEFAULT 1,
    price NUMERIC(12,2) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (cart_id, product_id)
);

CREATE TABLE IF NOT EXISTS wishlist (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    email VARCHAR(256) NULL,
    ip_address VARCHAR(100) NULL,
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS recent_views (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    email VARCHAR(256) NULL,
    ip_address VARCHAR(100) NULL,
    viewed_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_recent_views_user_product
    ON recent_views (product_id, COALESCE(email, ''), COALESCE(ip_address, ''));

CREATE TABLE IF NOT EXISTS reviews (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    email VARCHAR(256) NOT NULL,
    rating INT NOT NULL CHECK (rating >= 1 AND rating <= 5),
    comment TEXT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL
);

CREATE TABLE IF NOT EXISTS contact_us (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(120) NOT NULL,
    email VARCHAR(256) NOT NULL,
    phone VARCHAR(25) NULL,
    subject VARCHAR(255) NULL,
    message TEXT NOT NULL,
    status VARCHAR(40) NOT NULL DEFAULT 'NEW',
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS address_details (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL,
    full_name VARCHAR(100) NOT NULL,
    phone_number VARCHAR(15),
    address_line1 TEXT NOT NULL,
    address_line2 TEXT,
    city VARCHAR(50) NOT NULL,
    state VARCHAR(50) NOT NULL,
    country VARCHAR(50) DEFAULT 'India',
    postal_code VARCHAR(10) NOT NULL,
    address_type VARCHAR(20) CHECK (address_type IN ('Home', 'Office', 'Other')),
    is_default BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS coupons (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    code VARCHAR(80) NOT NULL UNIQUE,
    description TEXT NULL,
    discount_type VARCHAR(20) NOT NULL CHECK (discount_type IN ('percentage', 'fixed')),
    discount_value NUMERIC(12,2) NOT NULL,
    min_order_amount NUMERIC(12,2) NOT NULL DEFAULT 0,
    max_discount_amount NUMERIC(12,2) NULL,
    usage_limit INT NULL,
    start_date TIMESTAMP NOT NULL,
    end_date TIMESTAMP NOT NULL,
    applicable_on VARCHAR(40) NOT NULL DEFAULT 'all',
    created_by VARCHAR(256) NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS coupon_products (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    coupon_id UUID NOT NULL REFERENCES coupons(id) ON DELETE CASCADE,
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    UNIQUE (coupon_id, product_id)
);

CREATE TABLE IF NOT EXISTS coupon_usage (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    coupon_id UUID NOT NULL REFERENCES coupons(id) ON DELETE CASCADE,
    user_email VARCHAR(256) NOT NULL,
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    used_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS orders (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_email VARCHAR(256) NOT NULL,
    address_id UUID NULL REFERENCES address_details(id) ON DELETE SET NULL,
    total_amount NUMERIC(12,2) NOT NULL DEFAULT 0,
    discount_amount NUMERIC(12,2) NOT NULL DEFAULT 0,
    final_amount NUMERIC(12,2) NOT NULL DEFAULT 0,
    payment_method VARCHAR(30) NOT NULL,
    payment_status VARCHAR(30) NOT NULL DEFAULT 'PENDING',
    order_status VARCHAR(40) NOT NULL DEFAULT 'PLACED',
    razorpay_order_id VARCHAR(120) NULL,
    razorpay_payment_id VARCHAR(120) NULL,
    razorpay_signature TEXT NULL,
    coupon_code VARCHAR(80) NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS order_items (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    order_id UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    product_id UUID NOT NULL REFERENCES products(id),
    quantity INT NOT NULL DEFAULT 1,
    price NUMERIC(12,2) NOT NULL,
    discount NUMERIC(12,2) NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS payment_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    order_id UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    razorpay_order_id VARCHAR(120) NULL,
    razorpay_payment_id VARCHAR(120) NULL,
    razorpay_signature TEXT NULL,
    status VARCHAR(40) NOT NULL DEFAULT 'PENDING',
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS order_exchanges (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    order_id UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    order_item_id UUID NOT NULL REFERENCES order_items(id) ON DELETE CASCADE,
    user_email VARCHAR(256) NOT NULL,
    old_product_id UUID NOT NULL REFERENCES products(id),
    new_product_id UUID NOT NULL REFERENCES products(id),
    reason TEXT NULL,
    status VARCHAR(40) NOT NULL DEFAULT 'REQUESTED',
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS exchange_pickups (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    exchange_id UUID NOT NULL REFERENCES order_exchanges(id) ON DELETE CASCADE,
    order_id UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    pickup_address TEXT NOT NULL,
    pickup_date TIMESTAMP NOT NULL,
    status VARCHAR(40) NOT NULL DEFAULT 'SCHEDULED',
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS order_status_history (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    order_id UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    status VARCHAR(40) NOT NULL,
    updated_by_email VARCHAR(256) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS blogs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL,
    title VARCHAR(255) NOT NULL,
    slug VARCHAR(255) NOT NULL UNIQUE,
    description TEXT NULL,
    content TEXT NULL,
    image TEXT NULL,
    meta_title VARCHAR(255) NULL,
    meta_description TEXT NULL,
    tags TEXT[] NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL
);

CREATE TABLE IF NOT EXISTS banners (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    title VARCHAR(255) NOT NULL,
    image TEXT NOT NULL,
    link TEXT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL
);

CREATE TABLE IF NOT EXISTS articles (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    title VARCHAR(255) NOT NULL,
    slug VARCHAR(255) NOT NULL UNIQUE,
    description TEXT NULL,
    content TEXT NULL,
    image_url TEXT NULL,
    created_by VARCHAR(256) NULL,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL
);

CREATE TABLE IF NOT EXISTS delivery_pincodes (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    pincode VARCHAR(12) NOT NULL UNIQUE,
    city VARCHAR(100) NOT NULL,
    state VARCHAR(100) NOT NULL,
    is_serviceable BOOLEAN NOT NULL DEFAULT TRUE,
    delivery_days INT NOT NULL DEFAULT 0,
    created_by_email VARCHAR(256) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS "VendorBusinessDetails" (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "VendorId" TEXT NOT NULL,
    "BusinessName" TEXT NULL,
    "BusinessType" TEXT NULL,
    "BusinessCategory" TEXT NULL,
    "BusinessDescription" TEXT NULL,
    "GSTNumber" TEXT NULL,
    "PANNumber" TEXT NULL,
    "CINNumber" TEXT NULL,
    "UdyamRegistrationNumber" TEXT NULL,
    "AddressLine1" TEXT NULL,
    "AddressLine2" TEXT NULL,
    "City" TEXT NULL,
    "State" TEXT NULL,
    "Country" TEXT NULL DEFAULT 'India',
    "Pincode" TEXT NULL,
    "BusinessEmail" TEXT NULL,
    "BusinessPhone" TEXT NULL,
    "AlternatePhone" TEXT NULL,
    "WebsiteUrl" TEXT NULL,
    "GSTDocumentUrl" TEXT NULL,
    "PANDocumentUrl" TEXT NULL,
    "CINCertificateUrl" TEXT NULL,
    "BusinessLogoUrl" TEXT NULL,
    "IsVerified" BOOLEAN NOT NULL DEFAULT FALSE,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NULL
);

CREATE TABLE IF NOT EXISTS "VendorBankDetails" (
    "Id" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "VendorId" TEXT NOT NULL,
    "BankName" TEXT NOT NULL,
    "AccountHolderName" TEXT NOT NULL,
    "AccountNumber" TEXT NOT NULL,
    "IFSCCode" TEXT NOT NULL,
    "BranchName" TEXT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedDate" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Dummy data inserts: VendorBusinessDetails
INSERT INTO "VendorBusinessDetails"
("VendorId","BusinessName","BusinessType","BusinessCategory","BusinessDescription","GSTNumber","PANNumber","CINNumber","UdyamRegistrationNumber","AddressLine1","AddressLine2","City","State","Country","Pincode","BusinessEmail","BusinessPhone","AlternatePhone","WebsiteUrl","GSTDocumentUrl","PANDocumentUrl","CINCertificateUrl","BusinessLogoUrl","IsVerified","IsActive","UpdatedAt")
VALUES
('vendor_001','Shree Traders','Proprietorship','Electronics','Consumer electronics and accessories supplier','27ABCDE1234F1Z5','ABCDE1234F','U12345MH2022PTC123456','UDYAM-MH-01-0123456','Shop 12, Market Yard','Near Bus Stand','Pune','Maharashtra','India','411001','contact@shreetraders.in','9876543210','9876543211','https://shreetraders.in','https://cdn.example.com/docs/gst_vendor001.pdf','https://cdn.example.com/docs/pan_vendor001.pdf','https://cdn.example.com/docs/cin_vendor001.pdf','https://cdn.example.com/logo/vendor001.png',TRUE,TRUE,NOW()),
('vendor_002','GreenLeaf Supplies','Partnership','Home & Kitchen','Eco-friendly home and kitchen products','29PQRSX5678K1Z2','PQRSX5678K','U52100KA2021PTC987654','UDYAM-KR-02-0765432','45, MG Road','2nd Floor','Bengaluru','Karnataka','India','560001','hello@greenleafsupplies.com','9123456780','9123456781','https://greenleafsupplies.com','https://cdn.example.com/docs/gst_vendor002.pdf','https://cdn.example.com/docs/pan_vendor002.pdf','https://cdn.example.com/docs/cin_vendor002.pdf','https://cdn.example.com/logo/vendor002.png',FALSE,TRUE,NOW());

-- Dummy data inserts: VendorBankDetails
INSERT INTO "VendorBankDetails"
("VendorId","BankName","AccountHolderName","AccountNumber","IFSCCode","BranchName","IsActive")
VALUES
('vendor_001','State Bank of India','Shree Traders','123456789012','SBIN0001234','Pune Main Branch',TRUE),
('vendor_002','HDFC Bank','GreenLeaf Supplies','987654321098','HDFC0005678','MG Road Branch',TRUE);
