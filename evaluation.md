# Evaluation of AI Content Assistant 2.0

This document evaluates output quality, prompt techniques, example runs, limitations and a short conclusion. The goal is to show critical review required by the assignment.

## 1 — Quality criteria (what counts as "good")
- Relevance: directly answers the user prompt without unrelated content.
- Tone consistency: matches the requested tone parameter across headings and paragraphs.
- Format correctness: produces the requested structure (markdown headings, paragraphs, lists) or the required JSON schema fields when used.
- Uncertainty handling: when facts are unknown, the model returns an explicit uncertaintyFlag or plain language that it cannot verify.

## 2 — Prompting & safety techniques applied
- Developer system message uses explicit rules: required output fields (headline, paragraphs, uncertaintyFlag), maximum paragraphs, and instructions to state uncertainty when appropriate.
- The ContentAPI enforces a JSON schema at the LLM boundary (ProxyAPI) so responses conform to structure; SavedContentService validates and handles malformed responses gracefully.
- Simple rate/failure protection: AiProxyClient includes retry and a short circuit-breaker; CustomExceptionHandler maps upstream errors to ProblemDetails.

## 3 — Example prompts, sample outputs and analysis

Test 1 — Technical explanation
- Prompt: "Explain how dependency injection lifetimes work in ASP.NET Core"
- Tone: Professional
- Sample (simulated) output:

```
{ "headline": "Dependency Injection Lifetimes in ASP.NET Core",
  "paragraphs": [
	"Transient: created every time they're requested. Good for lightweight, stateless services.",
	"Scoped: one instance per HTTP request. Use for services that hold per-request state, like repositories.",
	"Singleton: one instance for application lifetime. Must be thread-safe and should not capture scoped services."
  ],
  "uncertaintyFlag": false
}
```

- Analysis: Response matches structural schema, tone is professional, factual integrity is correct. Minor improvements: add code samples or examples — handled via prompt extension.

Test 2 — Ambiguous / fictional content
- Prompt: "What are the key political achievements of the fictional country of Alvoria in 2024?"
- Tone: Informative
- Sample output:

```
{ "headline": "Uncertainty: Fictional Entity Detected",
  "paragraphs": ["I am uncertain about this topic. 'Alvoria' does not match known factual sources."] ,
  "uncertaintyFlag": true
}
```

- Analysis: Good behavior. The assistant avoids fabricating facts and sets uncertaintyFlag=true.

Test 3 — Marketing product description (creative but constrained)
- Prompt: "Write a short product description for a solar powered backpack. Keep it <= 3 short paragraphs, tone: Informative"
- Sample output:

```
{ "headline": "Solar-Powered Backpack — Clean Power On The Go",
  "paragraphs": [
	"Compact backpack with integrated flexible solar panel and 10,000mAh battery — charges phones and small devices.",
	"Water-resistant materials, ergonomic straps, and two USB outputs for simultaneous charging.",
	"Ideal for commuters and light travel; check local regulations for battery transport on flights."
  ],
  "uncertaintyFlag": false
}
```

- Analysis: Creative content is relevant and useful. The model also gives a safety hint (battery/flight rules) which is appropriate; factual claims like capacity should be verified in production.

## 4 — Failures observed and mitigation
- Hallucination risk: large factual claims (dates, statistics) can be invented — mitigation: require explicit uncertaintyFlag and post-generation verification step (human review) for factual claims.
- Format drift: when the upstream model returns plain text or code fences, the service strips fences and attempts to parse JSON; fallback returns raw cleaned text to the user rather than leaking internal errors.
- Rate-limits & timeouts: AiProxyClient includes retry and a short circuit-breaker; CustomExceptionHandler converts 429/504/502 to proper ProblemDetails.

## 5 — Practical recommendations
- For drafting content: use as-is with human review.
- For factual or safety-critical uses: add external verification step (fact-checking) and do not publish without human approval.

## 6 — Conclusion
The assistant performs well for structured drafting tasks and is engineered to reduce hallucinations by schema enforcement and uncertainty signaling. It is NOT a replacement for expert verification and should be used with human-in-the-loop review for any factual or safety-critical outputs.
