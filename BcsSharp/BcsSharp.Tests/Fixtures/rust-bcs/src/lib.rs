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
