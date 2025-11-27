-- Create payment_transaction table for PaymentService
-- Run this script manually in your PostgreSQL database

CREATE TABLE IF NOT EXISTS payment_transaction (
    id SERIAL PRIMARY KEY,
    order_id VARCHAR(100) NOT NULL UNIQUE,
    account_id INTEGER NOT NULL,
    partner_code VARCHAR(50) NOT NULL,
    request_id VARCHAR(100) NOT NULL UNIQUE,
    amount BIGINT NOT NULL,
    order_info VARCHAR(500) NOT NULL,
    payment_url TEXT,
    qr_code TEXT,
    status VARCHAR(20) NOT NULL DEFAULT 'PENDING',
    momo_transaction_id VARCHAR(100),
    response_code VARCHAR(50),
    message TEXT,
    signature TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    paid_at TIMESTAMP
);

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS idx_payment_transaction_account ON payment_transaction(account_id);
CREATE INDEX IF NOT EXISTS idx_payment_transaction_status ON payment_transaction(status);
CREATE INDEX IF NOT EXISTS idx_payment_transaction_order_id ON payment_transaction(order_id);
CREATE INDEX IF NOT EXISTS idx_payment_transaction_request_id ON payment_transaction(request_id);

