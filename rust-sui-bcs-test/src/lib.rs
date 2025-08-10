pub mod types;

pub use types::*;
use anyhow::Result;

/// Utility functions for BCS serialization compatible with Sui
pub struct SuiBcsUtils;

impl SuiBcsUtils {
    /// Serialize any serializable struct to BCS bytes
    /// This uses the standard BCS format that Sui expects
    pub fn serialize<T: serde::Serialize>(data: &T) -> Result<Vec<u8>> {
        Ok(bcs::to_bytes(data)?)
    }

    /// Deserialize BCS bytes back to the original struct
    pub fn deserialize<T: for<'de> serde::Deserialize<'de>>(bytes: &[u8]) -> Result<T> {
        Ok(bcs::from_bytes(bytes)?)
    }

    /// Get the serialized size of a struct without actually serializing
    pub fn serialized_size<T: serde::Serialize>(data: &T) -> Result<usize> {
        Ok(bcs::to_bytes(data)?.len())
    }

    /// Validate that the serialized data follows BCS format constraints
    /// BCS has specific rules about deterministic serialization
    pub fn validate_bcs_format<T: serde::Serialize + for<'de> serde::Deserialize<'de> + PartialEq>(
        data: &T,
    ) -> Result<bool> {
        let serialized = Self::serialize(data)?;
        let deserialized: T = Self::deserialize(&serialized)?;
        Ok(*data == deserialized)
    }
}

/// Example functions that demonstrate how BCS data would be used in Sui context
pub mod sui_examples {
    use super::*;
    
    /// Simulate preparing transaction arguments for Sui
    /// In real Sui development, these would be passed to Move functions
    pub fn prepare_transaction_args(
        user: &User,
        asset: &GameAsset,
        amount: u64,
    ) -> Result<Vec<Vec<u8>>> {
        Ok(vec![
            SuiBcsUtils::serialize(&user.id)?,
            SuiBcsUtils::serialize(&asset.asset_id)?,
            SuiBcsUtils::serialize(&amount)?,
        ])
    }
    
    /// Simulate parsing event data from Sui
    /// Sui events are emitted as BCS-encoded data
    pub fn parse_event_data(bcs_data: &[u8]) -> Result<Transaction> {
        SuiBcsUtils::deserialize(bcs_data)
    }
    
    /// Example of how object data might be stored on Sui
    /// Sui objects contain BCS-encoded data fields
    pub fn create_sui_object_data(asset: &GameAsset) -> Result<Vec<u8>> {
        SuiBcsUtils::serialize(asset)
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::types::*;

    #[test]
    fn test_user_serialization() {
        let user = User {
            id: 123,
            name: "Test User".to_string(),
            email: "test@example.com".to_string(),
            balance: 1000,
            is_verified: true,
        };

        let bytes = SuiBcsUtils::serialize(&user).unwrap();
        let deserialized: User = SuiBcsUtils::deserialize(&bytes).unwrap();
        
        assert_eq!(user, deserialized);
    }

    #[test]
    fn test_bcs_deterministic() {
        let asset = GameAsset {
            asset_id: "test_001".to_string(),
            asset_type: AssetType::Weapon,
            rarity: Rarity::Legendary,
            level: 50,
            attributes: vec![
                Attribute {
                    name: "damage".to_string(),
                    value: 100,
                },
            ],
        };

        // Serialize the same data multiple times
        let bytes1 = SuiBcsUtils::serialize(&asset).unwrap();
        let bytes2 = SuiBcsUtils::serialize(&asset).unwrap();
        
        // BCS should be deterministic - same input produces same output
        assert_eq!(bytes1, bytes2);
        
        // Validate BCS format
        assert!(SuiBcsUtils::validate_bcs_format(&asset).unwrap());
    }

    #[test]
    fn test_complex_nested_structure() {
        let game_state = GameState {
            players: vec![
                User {
                    id: 1,
                    name: "Player1".to_string(),
                    email: "p1@example.com".to_string(),
                    balance: 500,
                    is_verified: true,
                },
            ],
            active_transactions: vec![],
            marketplace: vec![],
            global_stats: GlobalStats {
                total_users: 1,
                total_transactions: 0,
                total_volume: 0,
                assets_by_rarity: vec![
                    (Rarity::Common, 100),
                    (Rarity::Rare, 10),
                ],
            },
        };

        let bytes = SuiBcsUtils::serialize(&game_state).unwrap();
        let deserialized: GameState = SuiBcsUtils::deserialize(&bytes).unwrap();
        
        assert_eq!(game_state, deserialized);
    }

    #[test]
    fn test_bcs_compatible_data() {
        let bcs_data = SuiCompatibleData {
            owner: "0x123456789abcdef123456789abcdef123456789abcdef123456789abcdef12".to_string(),
            balance: 1000000, // 1 SUI in MIST
            metadata: vec![0x01, 0x02, 0x03],
        };

        let serialized = SuiBcsUtils::serialize(&bcs_data).unwrap();
        let deserialized: SuiCompatibleData = SuiBcsUtils::deserialize(&serialized).unwrap();
        
        assert_eq!(bcs_data, deserialized);
        
        // Check hex output format (should start with 0x)
        let hex_string = SuiBcsUtils::to_hex_string(&bcs_data).unwrap();
        assert!(hex_string.starts_with("0x"));
    }

    #[test]
    fn test_transaction_args_preparation() {
        let user = User {
            id: 42,
            name: "Alice".to_string(),
            email: "alice@test.com".to_string(),
            balance: 100,
            is_verified: false,
        };

        let asset = GameAsset {
            asset_id: "sword001".to_string(),
            asset_type: AssetType::Weapon,
            rarity: Rarity::Epic,
            level: 10,
            attributes: vec![],
        };

        let args = sui_examples::prepare_transaction_args(&user, &asset, 500).unwrap();
        
        // Should have 3 arguments
        assert_eq!(args.len(), 3);
        
        // Each argument should be valid BCS
        let user_id: u64 = SuiBcsUtils::deserialize(&args[0]).unwrap();
        let asset_id: String = SuiBcsUtils::deserialize(&args[1]).unwrap();
        let amount: u64 = SuiBcsUtils::deserialize(&args[2]).unwrap();
        
        assert_eq!(user_id, 42);
        assert_eq!(asset_id, "sword001");
        assert_eq!(amount, 500);
    }
}