"""Validate the authored JSON catalogs without a Unity project or third-party packages."""

import json
from pathlib import Path
from string import Formatter


ROOT = Path(__file__).resolve().parents[1]
EXPECTED = {
    "companies": 12,
    "events": 24,
    "news": 48,
    "community_posts": 60,
    "community_comments": 96,
    "story_milestones": 4,
}


def require(value, message):
    if not value:
        raise ValueError(message)


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        require(key not in result, f"Duplicate JSON key: {key}")
        result[key] = value
    return result


def main():
    collections = {}
    all_ids = set()
    for name, count in EXPECTED.items():
        raw = (ROOT / "data" / f"{name}.json").read_text(encoding="utf-8")
        require("\ufffd" not in raw, f"Invalid Unicode in {name}")
        document = json.loads(raw, object_pairs_hook=unique_object)
        require(document["schema_version"] == 1, f"Unsupported schema: {name}")
        items = document["items"]
        require(len(items) == count, f"Unexpected initial catalog count: {name}")
        collections[name] = {item["id"]: item for item in items}
        require(len(collections[name]) == count, f"Duplicate IDs in {name}")
        for item in items:
            require(item["id"] not in all_ids, f"ID reused across catalogs: {item['id']}")
            all_ids.add(item["id"])

    companies = collections["companies"]
    events = collections["events"]
    news = collections["news"]
    posts = collections["community_posts"]
    comments = collections["community_comments"]
    require(len({x["symbol"] for x in companies.values()}) == len(companies), "Duplicate stock symbols")
    require(len({x["company_name"] for x in companies.values()}) == len(companies), "Duplicate company names")

    for event in events.values():
        require(event["company_ids"], f"No compatible companies: {event['id']}")
        require(set(event["company_ids"]) <= companies.keys(), f"Unknown company: {event['id']}")
        require(event["selection_weight"] is None, "Outcome weights need explicit balance design")
        for article_id in event["news_ids"]:
            article = news[article_id]
            require(article["fact_tag"] == event["id"], f"Wrong news fact: {article_id}")
            require(article["topic"] == event["topic"], f"Wrong news topic: {article_id}")
        for comment_id in event["comment_ids"]:
            comment = comments[comment_id]
            require(comment["required_public_fact"] == event["id"], f"Wrong comment fact: {comment_id}")
            require(comment["topic"] == event["topic"], f"Wrong comment topic: {comment_id}")
            require(set(comment["allowed_phases"]) <= {"official", "follow_up"}, f"Premature reveal: {comment_id}")

    for article in news.values():
        require(article["fact_tag"] in events, f"Unknown fact: {article['id']}")
    used_news = {i for e in events.values() for i in e["news_ids"]}
    require(used_news == news.keys(), "Orphan news templates")

    for comment in comments.values():
        require(comment["allowed_parent_ids"], f"No allowed posts: {comment['id']}")
        require(set(comment["allowed_parent_ids"]) <= posts.keys(), f"Unknown parent post: {comment['id']}")
        if comment["required_public_fact"]:
            require(comment["required_public_fact"] in events, "Unknown public fact")
            for parent_id in comment["allowed_parent_ids"]:
                require(posts[parent_id]["topic"] == comment["topic"], "Comment and post topics disagree")

    for post in posts.values():
        eligible = [c for c in comments.values() if post["id"] in c["allowed_parent_ids"]]
        require(eligible, f"No comment candidates: {post['id']}")
        if post["kind"] == "rumor_or_discussion":
            outcomes = [e for e in events.values() if e["topic"] == post["topic"]]
            require(len(outcomes) >= 2, f"Rumor maps to only one outcome: {post['id']}")

    for group in (news, posts, comments):
        full_texts = set()
        for item in group.values():
            text = item.get("title", "") + "\n" + item["body"]
            require(text not in full_texts, f"Duplicate full text: {item['id']}")
            full_texts.add(text)
            fields = {field for _, field, _, _ in Formatter().parse(text) if field}
            require(fields <= {"company_name", "product_name"}, f"Unknown placeholder: {item['id']}")
            for company in companies.values():
                rendered = text.format(**company)
                require("{" not in rendered and "}" not in rendered, f"Unresolved placeholder: {item['id']}")

    stories = sorted(collections["story_milestones"].values(), key=lambda x: x["order"])
    previous = None
    choices = set()
    for story in stories:
        require(story["requires_previous_milestone"] == previous, "Invalid story order")
        require(story["unconfigured_requirements_block_trigger"], "Unset thresholds must not auto-unlock")
        require(story["retain_unlock_after_loss"], "Unlocked story progress must persist")
        require(len(story["choices"]) == 2, "Initial story proposals need two choices")
        for choice in story["choices"]:
            require(choice["id"] not in choices, "Duplicate story choice")
            choices.add(choice["id"])
        previous = story["id"]

    print("PASS: JSON syntax, IDs, references, fact/phase compatibility, parent posts,")
    print("      unique text, placeholder rendering and story proposal order.")
    print(json.dumps({key: len(value) for key, value in collections.items()}))
    print("Market randomness, balance, save/load and Unity integration are not implemented or tested.")


if __name__ == "__main__":
    main()
