#!/usr/bin/env python3
"""
Script to generate a CSV file with transaction data including anomalies.
Generates 500 rows with approximately 5% anomalies.
"""

import csv
import random
from datetime import datetime, timedelta

# Sample data for realistic transactions
MERCHANTS = [
    "Amazon", "Walmart", "Starbucks", "McDonald's", "Target", "Home Depot",
    "Best Buy", "Costco", "CVS Pharmacy", "Walgreens", "Shell", "Exxon",
    "Whole Foods", "Trader Joe's", "Subway", "Chipotle", "Netflix", "Spotify",
    "Apple Store", "Microsoft Store", "Google Play", "Uber", "Lyft", "Airbnb",
    "Expedia", "Delta Airlines", "United Airlines", "American Airlines"
]

CATEGORIES = [
    "Groceries", "Restaurants", "Gas", "Shopping", "Entertainment", "Travel",
    "Utilities", "Healthcare", "Transportation", "Subscription", "Electronics",
    "Home Improvement", "Clothing", "Education", "Insurance"
]

# Merchant to category mapping for realistic categorization
MERCHANT_CATEGORY_MAP = {
    "Amazon": "Shopping",
    "Walmart": "Shopping",
    "Starbucks": "Restaurants",
    "McDonald's": "Restaurants",
    "Target": "Shopping",
    "Home Depot": "Home Improvement",
    "Best Buy": "Electronics",
    "Costco": "Groceries",
    "CVS Pharmacy": "Healthcare",
    "Walgreens": "Healthcare",
    "Shell": "Gas",
    "Exxon": "Gas",
    "Whole Foods": "Groceries",
    "Trader Joe's": "Groceries",
    "Subway": "Restaurants",
    "Chipotle": "Restaurants",
    "Netflix": "Subscription",
    "Spotify": "Subscription",
    "Apple Store": "Electronics",
    "Microsoft Store": "Electronics",
    "Google Play": "Subscription",
    "Uber": "Transportation",
    "Lyft": "Transportation",
    "Airbnb": "Travel",
    "Expedia": "Travel",
    "Delta Airlines": "Travel",
    "United Airlines": "Travel",
    "American Airlines": "Travel"
}

LOCATIONS_USA = [
    "New York, USA", "Los Angeles, USA", "Chicago, USA", "Houston, USA",
    "Miami, USA", "Seattle, USA", "Boston, USA", "San Francisco, USA"
]

LOCATIONS_INTERNATIONAL = [
    "Toronto, Canada", "Vancouver, Canada", "London, UK", "Paris, France",
    "Berlin, Germany", "Tokyo, Japan", "Sydney, Australia", "Dubai, UAE",
    "Singapore", "Mexico City, Mexico", "São Paulo, Brazil", "Buenos Aires, Argentina"
]

LOCATIONS = LOCATIONS_USA + LOCATIONS_INTERNATIONAL

def generate_normal_amount(category):
    """Generate a normal transaction amount based on category."""
    ranges = {
        "Groceries": (15, 200),
        "Restaurants": (8, 150),
        "Gas": (25, 80),
        "Shopping": (20, 500),
        "Entertainment": (10, 200),
        "Travel": (100, 2000),
        "Utilities": (50, 300),
        "Healthcare": (20, 500),
        "Transportation": (5, 100),
        "Subscription": (5, 50),
        "Electronics": (50, 2000),
        "Home Improvement": (30, 1000),
        "Clothing": (20, 400),
        "Education": (100, 2000),
        "Insurance": (50, 500)
    }
    min_amount, max_amount = ranges.get(category, (10, 200))
    return round(random.uniform(min_amount, max_amount), 2)

def generate_anomalous_amount():
    """Generate an unusually high transaction amount."""
    # Generate amounts between $5,000 and $50,000
    return round(random.uniform(5000, 50000), 2)

def generate_transactions(num_rows=500, anomaly_percentage=0.05):
    """Generate transaction data with anomalies."""
    base_date = datetime(2023, 1, 1)
    num_anomalies = int(num_rows * anomaly_percentage)
    
    transactions = []
    
    # Pre-plan anomaly positions and types
    anomaly_plan = {}
    anomaly_count = 0
    
    # Distribute anomaly types: high amounts, rapid location changes (pairs), repeated small (groups of 3-5)
    high_amount_count = num_anomalies // 3
    rapid_location_pairs = (num_anomalies // 3) // 2  # Each pair needs 2 transactions
    repeated_small_groups = (num_anomalies - high_amount_count - (rapid_location_pairs * 2)) // 3
    
    available_indices = list(range(num_rows))
    random.shuffle(available_indices)
    
    idx = 0
    
    # Assign high amount anomalies
    for _ in range(high_amount_count):
        if idx < len(available_indices):
            anomaly_plan[available_indices[idx]] = {"type": "high_amount"}
            idx += 1
    
    # Assign rapid location change pairs (need consecutive indices)
    for pair_num in range(rapid_location_pairs):
        if idx + 1 < len(available_indices):
            first_idx = available_indices[idx]
            second_idx = available_indices[idx + 1]
            anomaly_plan[first_idx] = {"type": "rapid_location_change", "pair_index": 0, "pair_id": pair_num}
            anomaly_plan[second_idx] = {"type": "rapid_location_change", "pair_index": 1, "pair_id": pair_num, "first_idx": first_idx}
            idx += 2
    
    # Assign repeated small transaction groups
    group_size = 3
    for _ in range(repeated_small_groups):
        if idx + group_size - 1 < len(available_indices):
            group_indices = available_indices[idx:idx + group_size]
            merchant = random.choice(MERCHANTS)
            location = random.choice(LOCATIONS)
            amount = round(random.uniform(0.99, 5.00), 2)
            base_time = base_date + timedelta(days=random.randint(0, 365), hours=random.randint(0, 23))
            
            for group_idx, plan_idx in enumerate(group_indices):
                anomaly_plan[plan_idx] = {
                    "type": "repeated_small",
                    "merchant": merchant,
                    "location": location,
                    "amount": amount,
                    "base_time": base_time,
                    "group_index": group_idx
                }
            idx += group_size
    
    # Generate transactions
    for i in range(num_rows):
        transaction_id = f"TXN{str(i+1).zfill(6)}"
        account_id = f"ACC{str(random.randint(1, 50)).zfill(4)}"
        
        if i in anomaly_plan:
            plan = anomaly_plan[i]
            anomaly_type = plan["type"]
            
            if anomaly_type == "high_amount":
                amount = generate_anomalous_amount()
                merchant = random.choice(MERCHANTS)
                category = MERCHANT_CATEGORY_MAP.get(merchant, random.choice(CATEGORIES))
                location = random.choice(LOCATIONS)
                timestamp = (base_date + timedelta(
                    days=random.randint(0, 365),
                    hours=random.randint(0, 23),
                    minutes=random.randint(0, 59)
                )).strftime("%Y-%m-%d %H:%M:%S")
                
            elif anomaly_type == "rapid_location_change":
                merchant = random.choice(MERCHANTS)
                category = MERCHANT_CATEGORY_MAP.get(merchant, random.choice(CATEGORIES))
                amount = generate_normal_amount(category)
                
                if plan["pair_index"] == 0:
                    # First transaction in USA
                    location = random.choice(LOCATIONS_USA)
                    timestamp_obj = base_date + timedelta(
                        days=random.randint(0, 365),
                        hours=random.randint(0, 23),
                        minutes=random.randint(0, 59)
                    )
                    # Store timestamp for the second transaction in the pair
                    plan["timestamp"] = timestamp_obj
                else:
                    # Second transaction in international location, within 5 minutes
                    location = random.choice(LOCATIONS_INTERNATIONAL)
                    first_idx = plan.get("first_idx")
                    first_plan = anomaly_plan.get(first_idx) if first_idx else None
                    
                    if first_plan and "timestamp" in first_plan:
                        # Second transaction happens within 1-5 minutes after the first
                        timestamp_obj = first_plan["timestamp"] + timedelta(minutes=random.randint(1, 5))
                    else:
                        # Fallback if first transaction not processed yet (shouldn't happen)
                        timestamp_obj = base_date + timedelta(
                            days=random.randint(0, 365),
                            hours=random.randint(0, 23),
                            minutes=random.randint(0, 59)
                        )
                
                timestamp = timestamp_obj.strftime("%Y-%m-%d %H:%M:%S")
                
            elif anomaly_type == "repeated_small":
                merchant = plan["merchant"]
                location = plan["location"]
                amount = plan["amount"]
                category = MERCHANT_CATEGORY_MAP.get(merchant, "Shopping")
                # Timestamps within 1 hour of each other
                group_minutes = plan["group_index"] * random.randint(5, 20)
                timestamp_obj = plan["base_time"] + timedelta(minutes=group_minutes)
                timestamp = timestamp_obj.strftime("%Y-%m-%d %H:%M:%S")
        else:
            # Generate normal transaction
            merchant = random.choice(MERCHANTS)
            category = MERCHANT_CATEGORY_MAP.get(merchant, random.choice(CATEGORIES))
            amount = generate_normal_amount(category)
            
            # Normal location distribution (weighted towards USA)
            if random.random() < 0.7:
                location = random.choice(LOCATIONS_USA)
            else:
                location = random.choice(LOCATIONS)
            
            timestamp = (base_date + timedelta(
                days=random.randint(0, 365),
                hours=random.randint(0, 23),
                minutes=random.randint(0, 59)
            )).strftime("%Y-%m-%d %H:%M:%S")
        
        transactions.append({
            "TransactionID": transaction_id,
            "AccountID": account_id,
            "Amount": amount,
            "Merchant": merchant,
            "Category": category,
            "Timestamp": timestamp,
            "Location": location
        })
    
    return transactions

def write_csv(transactions, filename="transactions.csv"):
    """Write transactions to CSV file."""
    fieldnames = ["TransactionID", "AccountID", "Amount", "Merchant", "Category", "Timestamp", "Location"]
    
    with open(filename, 'w', newline='', encoding='utf-8') as csvfile:
        writer = csv.DictWriter(csvfile, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(transactions)
    
    print(f"Successfully generated {len(transactions)} transactions in {filename}")

if __name__ == "__main__":
    # Set random seed for reproducibility (optional)
    random.seed(42)
    
    # Generate transactions
    transactions = generate_transactions(num_rows=500, anomaly_percentage=0.05)
    
    # Write to CSV
    write_csv(transactions, "transactions.csv")
    
    print("\nSample transactions:")
    for i, txn in enumerate(transactions[:5]):
        print(f"{i+1}. {txn['TransactionID']}: ${txn['Amount']} at {txn['Merchant']} ({txn['Location']})")
    
    # Show some high-value transactions (likely anomalies)
    high_value = [t for t in transactions if t['Amount'] > 5000]
    print(f"\nHigh-value transactions (>$5000): {len(high_value)}")
    if high_value:
        print("Sample high-value transactions:")
        for txn in high_value[:3]:
            print(f"  {txn['TransactionID']}: ${txn['Amount']} at {txn['Merchant']}")
