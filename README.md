# Basket Service

## About:

This is a simple Basket Api used for managing an online shopping basket with discounts, taxes and shipping.

## To Run

TODO

## (Author self notes beyond this point)
## Plan
### Criteria
##### We would like you to develop a REST based Web API for an example online shopping basket that has the following features:
- Add an item to the basket
- Add multiple items to the basket
- Add multiple of the same item to the basket
- Remove an item from the basket
- Get the total cost for the basket (including 20% VAT)
- Get the total cost without VAT
- Add a discounted item to the basket
	- (DB product table: productId: 123, basePrice: 10.00, discountedPrice: 8)
- (DB product table: productId: 456, basePrice: 5.00, discountedPrice: null)
- Add a discount code to the basket (excluding discounted items)
	- (DB discount code table: code: BLEH, type: percentage, value: 10)
- Add shipping cost to the UK
- Add shipping cost to other countries

### Assumptions
- Remove an item from the basket means one quantity of that item, not the whole thing
- Adding and removing items, discount codes and getting totals is likely called by a front end
    - However setting shipping is called by a trusted authenticated backend
    - On the frontend writes, don't trust the caller to give pricing information
    - We need a place in the backend to reference prices and discount code information, real world would likely be other api's, treat it this way but keep it simple in implementation.
- Discounted items have their discount applied to all items of that name
- We want to store shipping prices for multiple countries
- VAT is applied to shipping
- Discount code is applied before shipping and VAT
- Total = ((discounted prices * quantity) + (non discounted prices * quantity * (1 - discountCode)) + shipping) * 1.2

### Api
Domain: basket

POST /basket/items
[
    {
        "productId": "123",
        "quantity": 1
    },
    {
        "productId": "456",
        "quantity": 3
    },
    {
        "productId": "123",
        "quantity": 2
    }
]

PATCH /basket/items/123
{
    "quantityDelta": -1
}

GET /basket/totals?country=GB //required
{
    "subtotal": 50.00,
    "vatAmount": 10.00,
    "totalWithVat": 60.00
}

PUT /basket/discount-code
{
    "code": "BLEH"
}

PUT /basket/shipping/{countryCode}
{
    "cost": 2.50
}

### Internal basket model

basket:
{
    "items":
    [
        {
            "productId": "123",
            "quantity": 1,
            "unitPrice": 10.00, // dynamic
            "discountPrice": 5 // dynamic
        },
        {
            "productId": "456",
            "quantity": 3,
            "unitPrice": 10.00 // dynamic
        },
        {
            "productId": "123",
            "quantity": 2,
            "unitPrice": 10.00 // dynamic
        }
    ],
    "discountCode":
    {
        "code": "BLEH",
        "type": "percentage", // dynamic
        "value": 0.1 // dynamic
    }
    "shipping": {
        "GB": 2.50,
        "DE": 3,
        "FR": 7.20
    },
    "totals": { // all dynamic
        "subtotalBeforeDiscounts": 43.00,
        "itemDiscounts": 2.00,
        "discountCodeAmount": 3.50,
        "subtotalAfterDiscounts": 37.50,
        "vatOnItems": 7.50,
        "shippingCost": 2.50,
        "shippingVat": 0.50,
        "totalWithoutVat": 40.00,
        "totalWithVat": 48.00,
        "totalSavings": 5.50
    },
}

## Architecture
- Name: BasketService
- Only need a basket domain
- A single Cosmos DB with several containers
    - These serve different purposes so irl would be more than 1 DB
- Host a worker to write projections to the read only DB
    - Irl would have been it's own service
- Interact with a fake PricesApi and DiscountCodeApi
    - Will just store data in tables
- No need for integration events or message broker

Layout:
Temp
-> Temp.sln
-> README.md
-> Directory.Build.props
-> Directory.Packages.props
-> src/
----> Temp.api
----> Application
----> Domain
----> Infrastructure/
------> PricesApiClient
------> DiscountCodeApiClient
----> ProjectionWorker
-> tests/
-> openApiSpec
-> dockerCompose