# Microsoft.Bot.Core Architecture

This document describes the architecture of the Teams.NET Core bot framework located in the `core/` folder (from the `next/core` branch).

## Overview

The Microsoft.Bot.Core framework provides a lightweight, modern .NET implementation for building bot applications that communicate with Microsoft Bot Framework and Teams services. The architecture focuses on receiving HTTP activity requests and sending responses back to conversation endpoints.

## Main Components

```mermaid
graph TB
    subgraph "HTTP"
        HTTPReq[HTTP Request<br/>api/messages]
    end
    
    subgraph "Bot Application"
        BA[BotApplication]
        MW[Middleware Pipeline<br/>TurnMiddleware]
        Handler[OnActivity Handler]
        
        subgraph "Schema"
            Activity[CoreActivity]
            Conv[Conversation]
            Account[ConversationAccount]
        end
        
        subgraph "Communication"
            CC[ConversationClient]
            AuthHandler[BotAuthenticationHandler]
            HTTPClient[HttpClient]
        end
        
        subgraph "Authentication"
            MSAL[Token Acquisition<br/>MSAL]
        end
    end
    
    subgraph "Authorization"
        JWT[JWT Validation]
    end
    
    HTTPReq --> BA
    BA --> MW
    MW --> Handler
    Handler --> Activity
    Activity --> Conv
    Activity --> Account
    BA --> CC
    CC --> AuthHandler
    AuthHandler --> MSAL
    AuthHandler --> HTTPClient
    HTTPClient --> ServiceURL[activity.ServiceUrl<br/>v3/conversations]
    JWT -.validates.-> HTTPReq
```

## HTTP Request Flow (Receiving Messages)

This diagram shows how incoming HTTP requests are processed when a message arrives at the `api/messages` endpoint.

```mermaid
sequenceDiagram
    participant Client as Bot Framework Channel
    participant Endpoint as HTTP Endpoint<br/>api/messages
    participant Auth as JWT Authentication
    participant BotApp as BotApplication
    participant Pipeline as Middleware Pipeline
    participant Handler as OnActivity Handler
    
    Client->>Endpoint: POST /api/messages<br/>with Activity JSON + JWT
    Endpoint->>Auth: Validate JWT Token
    Auth-->>Endpoint: Token Valid
    Endpoint->>BotApp: ProcessAsync(HttpContext)
    BotApp->>BotApp: Deserialize CoreActivity<br/>from request body
    BotApp->>Pipeline: RunPipelineAsync(activity)
    
    loop For each middleware
        Pipeline->>Pipeline: Execute middleware.OnTurnAsync()
    end
    
    Pipeline->>Handler: Invoke OnActivity callback
    Handler->>Handler: Process activity<br/>(business logic)
    Handler-->>Pipeline: Complete
    Pipeline-->>BotApp: Complete
    BotApp-->>Endpoint: Return Activity ID
    Endpoint-->>Client: HTTP 200 OK
```

## Activity Message Sending Flow (to ServiceUrl)

This diagram shows how the bot sends activity messages back to the conversation endpoint.

```mermaid
sequenceDiagram
    participant Handler as OnActivity Handler
    participant BotApp as BotApplication
    participant ConvClient as ConversationClient
    participant HTTPClient as HttpClient
    participant AuthHandler as BotAuthenticationHandler
    participant MSAL as Token Acquisition<br/>(MSAL)
    participant ServiceURL as activity.ServiceUrl<br/>/v3/conversations/{id}/activities
    
    Handler->>BotApp: SendActivityAsync(activity)
    BotApp->>ConvClient: SendActivityAsync(activity)
    ConvClient->>ConvClient: Build URL from<br/>activity.ServiceUrl<br/>+ activity.Conversation.Id
    ConvClient->>HTTPClient: POST request with activity JSON
    HTTPClient->>AuthHandler: SendAsync()
    
    alt Agentic Identity (user-delegated)
        AuthHandler->>MSAL: Acquire token for user<br/>(agenticAppId, agenticUserId)
    else App-only
        AuthHandler->>MSAL: Acquire app-only token<br/>(client credentials)
    end
    
    MSAL-->>AuthHandler: Bearer token
    AuthHandler->>AuthHandler: Add Authorization header
    AuthHandler->>ServiceURL: POST activity with auth token
    ServiceURL-->>AuthHandler: HTTP 200 + ResourceResponse
    AuthHandler-->>HTTPClient: Response
    HTTPClient-->>ConvClient: ResourceResponse
    ConvClient-->>BotApp: ResourceResponse
    BotApp-->>Handler: ResourceResponse
```

## Middleware Pipeline

The middleware pipeline allows interception and processing of activities before they reach the bot handler.

```mermaid
graph LR
    subgraph "Middleware Pipeline Execution"
        Start[Incoming Activity] --> MW1[Middleware 1<br/>OnTurnAsync]
        MW1 --> MW2[Middleware 2<br/>OnTurnAsync]
        MW2 --> MW3[Middleware N<br/>OnTurnAsync]
        MW3 --> Handler[OnActivity<br/>Handler]
        Handler --> MW3R[Middleware N<br/>Complete]
        MW3R --> MW2R[Middleware 2<br/>Complete]
        MW2R --> MW1R[Middleware 1<br/>Complete]
        MW1R --> End[Response]
    end
    
    style Handler fill:#90EE90
    style Start fill:#87CEEB
    style End fill:#87CEEB
```

## Inbound Authentication Flow

This diagram shows how incoming HTTP requests are authenticated using JWT tokens.

```mermaid
graph TB
    InReq[Incoming HTTP Request] --> JWTVal[JWT Validation]
    JWTVal --> IssuerCheck{Issuer?}
    IssuerCheck -->|BotFramework| BotOIDC[Bot Framework OIDC]
    IssuerCheck -->|Azure AD| AadOIDC[Azure AD OIDC]
    BotOIDC --> ValidateToken[Validate Token]
    AadOIDC --> ValidateToken
    ValidateToken --> Authorized[Authorized Request]
    
    style InReq fill:#87CEEB
    style Authorized fill:#90EE90
```

## Outbound Authentication Flow

This diagram shows how the bot authenticates when sending messages to the Bot Framework service.

```mermaid
graph TB
    OutReq[Outgoing HTTP Request] --> IdentityCheck{Identity Type?}
    IdentityCheck -->|Agentic| UserToken[User-Delegated Token<br/>OBO Flow]
    IdentityCheck -->|App-only| AppToken[App-only Token<br/>Client Credentials]
    UserToken --> MSALToken[MSAL Token Acquisition]
    AppToken --> MSALToken
    MSALToken --> CredCheck{Credential Type?}
    CredCheck -->|Secret| ClientSecret[Client Secret]
    CredCheck -->|Managed Identity| UMI[User Assigned MI]
    CredCheck -->|FIC| FIC[Federated Identity]
    ClientSecret --> AddAuthHeader[Add Authorization Header]
    UMI --> AddAuthHeader
    FIC --> AddAuthHeader
    AddAuthHeader --> SendReq[Send Request to ServiceUrl]
    
    style OutReq fill:#87CEEB
    style SendReq fill:#90EE90
```

## Key Components Description

### BotApplication
- **Purpose**: Main entry point for bot functionality
- **Responsibilities**:
  - Processes incoming HTTP requests containing activities
  - Manages middleware pipeline execution
  - Provides methods to send activities back to conversations
  - Handles authentication configuration

### CoreActivity
- **Purpose**: Represents a bot activity (message, event, etc.)
- **Key Properties**:
  - `Type`: Activity type (message, typing, conversationUpdate, etc.)
  - `Text`: Message content
  - `ServiceUrl`: Endpoint URL for sending responses
  - `Conversation`: Conversation context
  - `From`/`Recipient`: Participant accounts
  - `ChannelData`: Channel-specific data

### ConversationClient
- **Purpose**: Sends activities to conversation endpoints
- **Responsibilities**:
  - Constructs the target URL from activity.ServiceUrl
  - Serializes activities to JSON
  - Uses authenticated HttpClient to POST activities
  - Returns ResourceResponse with activity ID

### Middleware Pipeline (TurnMiddleware)
- **Purpose**: Provides extensibility through middleware chain
- **Features**:
  - Sequential execution of registered middleware
  - Each middleware can inspect/modify activities
  - Common uses: logging, typing indicators, state management

### BotAuthenticationHandler
- **Purpose**: Handles outbound authentication for Bot Framework API calls
- **Features**:
  - Acquires OAuth tokens using MSAL
  - Supports app-only (client credentials) authentication
  - Supports agentic (user-delegated) authentication via OBO flow
  - Supports multiple credential types: secret, managed identity, federated identity
  - Attaches Bearer token to outgoing requests

### JWT Authentication
- **Purpose**: Validates incoming requests from Bot Framework
- **Features**:
  - Validates JWT tokens from Bot Framework or Azure AD
  - Dynamic OIDC configuration based on token issuer
  - Supports both bot and agent authentication schemes

## Request/Response Flow Summary

1. **Incoming Request**: Bot Framework posts activity to `api/messages`
2. **Authentication**: JWT token validated against OIDC configuration
3. **Deserialization**: Request body deserialized to CoreActivity
4. **Middleware Pipeline**: Activity passed through registered middleware
5. **Handler Execution**: OnActivity callback processes the activity
6. **Send Response**: Bot creates reply activity and calls SendActivityAsync
7. **Outbound Auth**: Token acquired via MSAL for the ServiceUrl
8. **HTTP POST**: Activity sent to `{ServiceUrl}/v3/conversations/{id}/activities`
9. **Response**: ResourceResponse with activity ID returned

## Configuration

The framework supports multiple configuration approaches:
- **Bot Framework Config**: MicrosoftAppId, MicrosoftAppPassword, MicrosoftAppTenantId
- **Core Config**: CLIENT_ID, CLIENT_SECRET, TENANT_ID
- **Azure AD Section**: AzureAd:ClientId, AzureAd:ClientSecret, etc.

Authentication credentials support:
- Client Secret (development)
- User-Assigned Managed Identity (production)
- Federated Identity Credential with Managed Identity (Azure workload identity)
