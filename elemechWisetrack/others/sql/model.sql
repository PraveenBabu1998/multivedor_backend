CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TABLE address_details (
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

    address_type VARCHAR(20)
        CHECK (address_type IN ('Home', 'Office', 'Other')),

    is_default BOOLEAN DEFAULT FALSE,

    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
