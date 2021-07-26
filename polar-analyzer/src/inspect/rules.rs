use serde::{Deserialize, Serialize};

use polar_core::{kb::KnowledgeBase, rules::Rule, sources::SourceInfo, terms::ToPolarString};

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct RuleInfo {
    pub symbol: String,
    pub signature: String,
    pub location: (Option<String>, usize, usize),
}

/// Get the string formatted signature of the rule
///
/// Either uses the source directly if it's available
/// (should usually be the case). Otherwise, construct it
/// from the name and parameters.
fn get_rule_signature(kb: &KnowledgeBase, r: &Rule) -> String {
    if let SourceInfo::Parser {
        src_id,
        left,
        right,
    } = r.source_info
    {
        let source = kb.sources.get_source(src_id);
        if let Some(source) = source {
            return source.src.chars().take(right).skip(left).collect();
        }
    }
    format!(
        "{}({})",
        r.name,
        r.params
            .iter()
            .map(|p| p.to_polar())
            .collect::<Vec<String>>()
            .join(", "),
    )
}

/// Get the location of the rule
fn get_rule_location(kb: &KnowledgeBase, r: &Rule) -> (Option<String>, usize, usize) {
    if let SourceInfo::Parser {
        src_id,
        left,
        right,
    } = r.source_info
    {
        let source = kb.sources.get_source(src_id);
        if let Some(source) = source {
            return (source.filename, left, right);
        }
    }
    (None, 0, 0)
}

pub fn get_rule_information(kb: &KnowledgeBase) -> Vec<RuleInfo> {
    kb.rules
        .iter()
        .flat_map(|(name, generic_rule)| {
            generic_rule.rules.iter().map(move |(_, r)| RuleInfo {
                symbol: name.0.clone(),
                signature: get_rule_signature(kb, r),
                location: get_rule_location(kb, r),
            })
        })
        .collect()
}