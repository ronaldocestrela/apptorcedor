# Documentação para calcular frete no melhor envio

## API de consulta
### metodo: post
### url: https://www.melhorenvio.com.br/api/v2/me/shipment/calculate
### Headers
    - Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJS...
    - User-Agent: Aplicação ronaldoestrela@yahoo.com.br
### Body
```
{
  "from": {
    "postal_code": "44085520" //Cep fixo 
  },
  "to": {
    "postal_code": "44088698" //Codigo postal do socio torcedor
  },
  "package": {
    "height": 4,
    "width": 12,
    "length": 17,
    "weight": 0.3
  }
}
```


### Resposta
```
[
    {
        "id": 1,
        "name": "PAC",
        "company": {
            "id": 1,
            "name": "Correios",
            "picture": "https://www.melhorenvio.com.br/images/shipping-companies/correios.png"
        },
        "error": "Transportadora não atende este trecho."
    },
    {
        "id": 2,
        "name": "SEDEX",
        "price": "12.68",
        "custom_price": "12.68",
        "discount": "0.00",
        "currency": "R$",
        "delivery_time": 2,
        "delivery_range": {
            "min": 1,
            "max": 2
        },
        "custom_delivery_time": 2,
        "custom_delivery_range": {
            "min": 1,
            "max": 2
        },
        "packages": [
            {
                "price": "12.68",
                "discount": "0.00",
                "format": "box",
                "weight": "0.30",
                "insurance_value": "0.00",
                "dimensions": {
                    "height": 4,
                    "width": 12,
                    "length": 17
                }
            }
        ],
        "additional_services": {
            "receipt": false,
            "own_hand": false,
            "collect": false
        },
        "additional": {
            "unit": {
                "price": 0,
                "delivery": 0
            }
        },
        "company": {
            "id": 1,
            "name": "Correios",
            "picture": "https://www.melhorenvio.com.br/images/shipping-companies/correios.png"
        }
    },
    {
        "id": 3,
        "name": ".Package",
        "price": "16.91",
        "custom_price": "16.91",
        "discount": "0.00",
        "currency": "R$",
        "delivery_time": 6,
        "delivery_range": {
            "min": 5,
            "max": 6
        },
        "custom_delivery_time": 6,
        "custom_delivery_range": {
            "min": 5,
            "max": 6
        },
        "packages": [
            {
                "format": "box",
                "weight": "0.30",
                "insurance_value": "0.00",
                "dimensions": {
                    "height": 4,
                    "width": 12,
                    "length": 17
                }
            }
        ],
        "additional_services": {
            "receipt": false,
            "own_hand": false,
            "collect": false
        },
        "additional": {
            "unit": {
                "price": 0,
                "delivery": 0
            }
        },
        "company": {
            "id": 2,
            "name": "Jadlog",
            "picture": "https://www.melhorenvio.com.br/images/shipping-companies/jadlog.png"
        }
    },
    {
        "id": 4,
        "name": ".Com",
        "price": "21.21",
        "custom_price": "21.21",
        "discount": "0.00",
        "currency": "R$",
        "delivery_time": 5,
        "delivery_range": {
            "min": 4,
            "max": 5
        },
        "custom_delivery_time": 5,
        "custom_delivery_range": {
            "min": 4,
            "max": 5
        },
        "packages": [
            {
                "format": "box",
                "weight": "0.30",
                "insurance_value": "0.00",
                "dimensions": {
                    "height": 4,
                    "width": 12,
                    "length": 17
                }
            }
        ],
        "additional_services": {
            "receipt": false,
            "own_hand": false,
            "collect": false
        },
        "additional": {
            "unit": {
                "price": 0,
                "delivery": 0
            }
        },
        "company": {
            "id": 2,
            "name": "Jadlog",
            "picture": "https://www.melhorenvio.com.br/images/shipping-companies/jadlog.png"
        }
    },
    {
        "id": 12,
        "name": "éFácil",
        "company": {
            "id": 6,
            "name": "LATAM Cargo",
            "picture": "https://www.melhorenvio.com.br/images/shipping-companies/latamcargo.png"
        },
        "error": "Transportadora não atende este trecho."
    },
    {
        "id": 15,
        "name": "Expresso",
        "company": {
            "id": 9,
            "name": "Azul Cargo Express",
            "picture": "https://www.melhorenvio.com.br/images/shipping-companies/azulcargo.png"
        },
        "error": "Transportadora não atende este trecho."
    },
    {
        "id": 16,
        "name": "e-commerce",
        "company": {
            "id": 9,
            "name": "Azul Cargo Express",
            "picture": "https://www.melhorenvio.com.br/images/shipping-companies/azulcargo.png"
        },
        "error": "Transportadora não atende este trecho."
    },
    {
        "id": 17,
        "name": "Mini Envios",
        "company": {
            "id": 1,
            "name": "Correios",
            "picture": "https://www.melhorenvio.com.br/images/shipping-companies/correios.png"
        },
        "error": "Transportadora não atende este trecho."
    },
    {
        "id": 22,
        "name": "Rodoviário",
        "company": {
            "id": 12,
            "name": "Buslog",
            "picture": "https://www.melhorenvio.com.br/images/shipping-companies/buslog.png"
        },
        "error": "Transportadora não atende este trecho."
    },
    {
        "id": 27,
        "name": ".Package Centralizado",
        "price": "15.97",
        "custom_price": "15.97",
        "discount": "0.00",
        "currency": "R$",
        "delivery_time": 6,
        "delivery_range": {
            "min": 5,
            "max": 6
        },
        "custom_delivery_time": 6,
        "custom_delivery_range": {
            "min": 5,
            "max": 6
        },
        "packages": [
            {
                "format": "box",
                "weight": "0.30",
                "insurance_value": "0.00",
                "dimensions": {
                    "height": 4,
                    "width": 12,
                    "length": 17
                }
            }
        ],
        "additional_services": {
            "receipt": false,
            "own_hand": false,
            "collect": false
        },
        "additional": {
            "unit": {
                "price": 0,
                "delivery": 0
            }
        },
        "company": {
            "id": 2,
            "name": "Jadlog",
            "picture": "https://www.melhorenvio.com.br/images/shipping-companies/jadlog.png"
        }
    },
    {
        "id": 31,
        "name": "Express",
        "price": "14.13",
        "custom_price": "14.13",
        "discount": "0.00",
        "currency": "R$",
        "delivery_time": 4,
        "delivery_range": {
            "min": 3,
            "max": 4
        },
        "custom_delivery_time": 4,
        "custom_delivery_range": {
            "min": 3,
            "max": 4
        },
        "packages": [
            {
                "price": "14.13",
                "discount": "0.00",
                "format": "box",
                "weight": "0.30",
                "insurance_value": "1.00",
                "dimensions": {
                    "height": 4,
                    "width": 12,
                    "length": 17
                }
            }
        ],
        "additional_services": {
            "receipt": false,
            "own_hand": false,
            "collect": false
        },
        "additional": {
            "unit": {
                "price": 0,
                "delivery": 0
            }
        },
        "company": {
            "id": 14,
            "name": "Loggi",
            "picture": "https://www.melhorenvio.com.br/images/shipping-companies/loggi.png"
        }
    },
    {
        "id": 32,
        "name": "Coleta",
        "company": {
            "id": 14,
            "name": "Loggi",
            "picture": "https://www.melhorenvio.com.br/images/shipping-companies/loggi.png"
        },
        "error": "Transportadora não atende este trecho."
    },
    {
        "id": 33,
        "name": "Standard",
        "price": "31.58",
        "custom_price": "31.58",
        "discount": "0.00",
        "currency": "R$",
        "delivery_time": 4,
        "delivery_range": {
            "min": 3,
            "max": 4
        },
        "custom_delivery_time": 4,
        "custom_delivery_range": {
            "min": 3,
            "max": 4
        },
        "packages": [
            {
                "price": "31.58",
                "discount": "0.00",
                "format": "box",
                "weight": "0.30",
                "insurance_value": "0.00",
                "dimensions": {
                    "height": 4,
                    "width": 12,
                    "length": 17
                }
            }
        ],
        "additional_services": {
            "receipt": false,
            "own_hand": false,
            "collect": false
        },
        "additional": {
            "unit": {
                "price": 0,
                "delivery": 0
            }
        },
        "company": {
            "id": 15,
            "name": "JeT",
            "picture": "https://www.melhorenvio.com.br/images/shipping-companies/jet.png"
        }
    },
    {
        "id": 34,
        "name": "Loggi Ponto",
        "price": "26.79",
        "custom_price": "26.79",
        "discount": "0.00",
        "currency": "R$",
        "delivery_time": 8,
        "delivery_range": {
            "min": 7,
            "max": 8
        },
        "custom_delivery_time": 8,
        "custom_delivery_range": {
            "min": 7,
            "max": 8
        },
        "packages": [
            {
                "price": "26.79",
                "discount": "0.00",
                "format": "box",
                "weight": "0.30",
                "insurance_value": "1.00",
                "dimensions": {
                    "height": 4,
                    "width": 12,
                    "length": 17
                }
            }
        ],
        "additional_services": {
            "receipt": false,
            "own_hand": false,
            "collect": false
        },
        "additional": {
            "unit": {
                "price": 0,
                "delivery": 0
            }
        },
        "company": {
            "id": 14,
            "name": "Loggi",
            "picture": "https://www.melhorenvio.com.br/images/shipping-companies/loggi.png"
        }
    },
    {
        "id": 35,
        "name": "Standard",
        "company": {
            "id": 8,
            "name": "Total Express",
            "picture": "https://www.melhorenvio.com.br/images/shipping-companies/totalexpress.png"
        },
        "error": "Transportadora não atende este trecho."
    }
]
```

## Integração no AppTorcedor

- **Config:** seção `MelhorEnvio` em `appsettings` (`Token`, `UserAgent`, `FromPostalCode`; opcionais `BaseUrl`, `PackageHeight`, `PackageWidth`, `PackageLength`, `PackageWeight`). **Produção / deploy:** variáveis `MelhorEnvio__*` em `api.env` (manual ou geradas pelo Jenkins: credenciais `melhor-envio-token`, `melhor-envio-user-agent`, `melhor-envio-from-postal-code` — linhas só gravadas quando não vazias); **Docker Compose:** `MELHOR_ENVIO_TOKEN`, `MELHOR_ENVIO_USER_AGENT`, `MELHOR_ENVIO_FROM_POSTAL_CODE` no `.env` da raiz. Ver `deploy/vps/api.env.example`, [guia-deploy.md](../deploy/guia-deploy.md) e [`Jenkinsfile`](../../Jenkinsfile).
- **API torcedor:** `GET /api/benefits/shipping-options?cep=` (JWT); retorno vazio se `Token` não configurado ou erro na Melhor Envio.
- **Persistência:** solicitação de camisa grava em `BenefitRedemptions` os campos `ShippingMethod` (`pickup` \| `carrier`), dados do serviço cotado e valores de frete quando `carrier`.