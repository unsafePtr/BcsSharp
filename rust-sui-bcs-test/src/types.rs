use serde::{Deserialize, Serialize};
use primitive_types::U256;

/// Represents a user in the system
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct User {
    pub id: u64,
    pub name: String,
    pub email: Option<String>,
    pub balance: U256,
    pub is_verified: bool,
    pub address: Option<Address>,
}

/// Represents an address for a user
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct  Address {
    pub street: String,
    pub city: String,
    pub state: Option<String>,
    pub zip: String,
}

/// Different types of game assets
/// C-style enums for asset categorization
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub enum AssetType {
    Weapon,
    Armor,
    Consumable,
    Material,
    Currency,
}

/// Asset attributes (like damage, defense, etc.)
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct Attribute {
    pub name: String,
    pub value: u32,
}

/// Represents a game asset/item
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct GameAsset {
    pub asset_id: String,
    pub asset_type: AssetType,
    pub level: u8,
    pub attributes: Vec<Attribute>,
}

/// Transaction types supported by the system
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub enum TransactionType {
    Transfer,
    Purchase,
    Sale,
    Mint,
    Burn,
}

/// Represents a transaction between users
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct Transaction {
    pub tx_id: String,
    pub from_user: u64,
    pub to_user: u64,
    pub amount: u64,
    pub timestamp: u64,
    pub tx_type: TransactionType,
}

/// Represents an item listed on a marketplace
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct MarketplaceItem {
    pub item_id: String,
    pub seller: u64,
    pub asset: GameAsset,
    pub price: u64,
    pub listed_at: u64,
    pub is_active: bool,
}

/// Complex nested structure for advanced serialization testing
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct GameState {
    pub players: Vec<User>,
    pub active_transactions: Vec<Transaction>,
    pub marketplace: Vec<MarketplaceItem>,
    pub global_stats: GlobalStats,
}

/// legendary asset with additional properties
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct LegendaryAsset {
    pub asset_id: String,
    pub level: u8
}

/// rarity levels for game assets
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub enum Rarity {
    Common,
    Uncommon(u32),
    Rare(Vec<u32>),
    Epic(String),
    Legendary((String, u64, LegendaryAsset)),
}

/// Global game statistics
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct GlobalStats {
    pub total_users: u64,
    pub total_transactions: u64,
    pub total_volume: u64,
    pub assets_by_rarity: Vec<(Rarity, u64)>,
}

/// Sui-compatible data structure demonstrating proper BCS usage
/// This follows Sui's conventions for on-chain data
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct SuiCompatibleData {
    /// Owner address (32-byte hex string representation)
    pub owner: String,
    /// Balance in MIST (smallest unit of SUI)
    pub balance: u64,
    /// Arbitrary metadata as raw bytes
    pub metadata: Vec<u8>,
}

/// Example of a struct that could represent a Sui object
/// Note: Real Sui objects would use UID instead of String for id
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct SuiObjectExample {
    /// Object ID (would be UID in real Sui Move)
    pub id: String,
    /// Version of the object
    pub version: u64,
    /// Object owner
    pub owner: String,
    /// Object data
    pub data: Vec<u8>,
}

/// Tuple examples for BCS serialization testing
/// Tuples are serialized as structs without field names in BCS
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct TupleExamples {
    /// Simple pair (string, number)
    pub simple_pair: (String, u32),
    /// Triple with different types
    pub triple: (u64, bool, String),
    /// Nested tuple with GameAsset
    pub nested_tuple: (String, GameAsset),
    /// Tuple with array
    pub tuple_with_array: (String, Vec<u32>),
    /// Complex tuple
    pub complex_tuple: (User, Transaction, bool),
}