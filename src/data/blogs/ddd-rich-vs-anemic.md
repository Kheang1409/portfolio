---
title: "DDD: Rich Domain Model vs Anemic Domain Model"
date: "2025-11-11"
slug: "ddd-rich-vs-anemic"
author: "Kai"
tags:
  - ddd
  - architecture
  - design
  - domain-model
---

> Short summary: Rich domain models encapsulate behavior and data together (the object is responsible for its invariants). Anemic models keep data and behavior separate (DTOs/entities are data-only). Each approach has trade-offs — this post explains both, shows examples, and gives guidance for choosing or migrating.

## What is an Anemic Domain Model?

An Anemic Domain Model keeps entities as data containers (properties only). Business logic and rules live in separate services or application layers. This can be simple and straightforward, but risks scattering rules across services and losing encapsulation.

Pros:

- Simple DTO-like entities
- Easy to serialize/deserialize and map to persistence
- Familiar to teams coming from CRUD/service-oriented patterns

Cons:

- Business rules are often duplicated or scattered
- Harder to guarantee object invariants
- Tests can become larger (need to set up data and call services)

Example (C# - anemic):

```csharp
public class Order {
  public Guid Id { get; set; }
  public List<OrderLine> Lines { get; set; }
  public decimal Total { get; set; }
}

public class OrderService {
  public void AddItem(Order order, Product p, int qty) {
    // service mutates Order and updates Total
  }
}
```

## What is a Rich (Behaviorful) Domain Model?

A Rich Domain Model puts behavior inside the domain objects. Entities enforce their own invariants and provide meaningful methods to operate on them. This matches the core DDD idea: the domain model expresses the domain language and protects its invariants.

Pros:

- Encapsulation: invariants are enforced in one place
- Clear, intention-revealing APIs (Order.AddItem())
- Easier to reason about correctness and test behavior directly

Cons:

- Slightly more design effort initially
- Need clear boundaries to avoid anemic services hiding inside objects

Example (C# - rich):

```csharp
public class Order {
  private readonly List<OrderLine> _lines = new();
  public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

  public void AddItem(Product p, int qty) {
    if (qty <= 0) throw new ArgumentException("qty");
    var existing = _lines.FirstOrDefault(l => l.ProductId == p.Id);
    if (existing != null) existing.Increase(qty);
    else _lines.Add(new OrderLine(p.Id, p.Price, qty));
  }

  public decimal Total => _lines.Sum(l => l.Subtotal);
}
```

Here `Order` manages its lines and computes the total — callers can't put the `Order` into an invalid state without calling its API.

## Choosing between them

- Start with the domain complexity: If your domain has non-trivial rules and invariants, prefer a rich model.
- If your application is primarily CRUD with thin business logic, an anemic model with services can be pragmatic.
- Teams moving to DDD often start with anemic models and gradually introduce rich behaviors where it matters.

## Migration tips (Anemic → Rich)

1. Identify invariants & business rules currently enforced in services or spread across code.
2. Introduce small, focused domain methods that encapsulate those rules (e.g., `Order.AddItem`, `Customer.CanPlaceOrder`).
3. Keep persistence mapping straightforward: repositories still persist entities, but call domain methods instead of manipulating properties directly.
4. Add tests around domain behaviors (unit tests on the domain object) before removing service-level logic.

## Small JS/TS example (Rich model flavor)

```ts
class Order {
  private lines: OrderLine[] = [];

  addItem(product: Product, qty: number) {
    if (qty <= 0) throw new Error("qty must be > 0");
    const item = this.lines.find((l) => l.productId === product.id);
    if (item) item.qty += qty;
    else this.lines.push({ productId: product.id, price: product.price, qty });
  }

  total() {
    return this.lines.reduce((s, l) => s + l.price * l.qty, 0);
  }
}
```

## When to avoid a rich model

- Extremely simple CRUD apps where all business logic is in stored procedures or in a gateway layer.
- When team familiarity and delivery speed require minimal upfront design — but add targeted rich models where bugs or duplication appear.

## Summary

Both patterns are valid. Prefer a rich, behaviorful domain model when domain rules, invariants, and maintainability matter. Use anemic models pragmatically for simple data-centric applications, but remain ready to extract domain behaviors into objects as the system grows.
