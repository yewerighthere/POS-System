# 📊 SmartPOS Processing Flow Analysis (Luồng Xử Lý)

## 🎯 System Overview

SmartPOS là hệ thống quản lý thanh toán và hóa đơn gồm 3 module độc lập nhưng có liên kết:

| Module | Port | Chức Năng |
|--------|------|----------|
| **VNPay** | 8081 | Xử lý thanh toán qua VNPay gateway |
| **Invoice** | 8082 | Quản lý tạo và xuất hóa đơn PDF |
| **Print** | 8083 | Quản lý hàng đợi in ấn |

---

## 🔄 Complete End-to-End Processing Flow

```mermaid
graph TD
    A["Customer Places Order"] --> B["📱 Payment Initiation"]
    B --> C["VNPay Module: Create Payment Request"]
    C --> D["Generate QR Code / Payment URL"]
    D --> E["Customer Scans QR / Enters Pin"]
    E --> F{"VNPay Gateway<br/>Processes Payment"}
    F -->|Success| G["VNPay: Callback Handler<br/>Verifies Signature"]
    F -->|Failed| H["Payment Failed"]
    G --> I["Update Payment Status<br/>to COMPLETED"]
    I --> J["📄 Invoice Module Triggered"]
    J --> K["Create Invoice Record"]
    K --> L["Calculate Totals & Tax"]
    L --> M["Save to Database"]
    M --> N["Generate Invoice PDF<br/>via PDFGenerationService"]
    N --> O["Store PDF Path"]
    O --> P["🖨️ Print Module"]
    P --> Q["Submit Print Job<br/>to Queue"]
    Q --> R{"Printer<br/>Available?"}
    R -->|Yes| S["Print Document"]
    R -->|Queue| T["Wait in Queue"]
    S --> U["Update Job Status: COMPLETED"]
    T --> U
    U --> V["✅ Complete"]
    H --> W["Notify Customer"]
    V --> W
```

---

## 📦 Module 1: VNPay Payment Processing

### 1.1 Payment Creation Flow

```mermaid
graph TD
    A["POST /api/v1/payments/create"] --> B["PaymentController.createPayment"]
    B --> C["Validate PaymentRequestDTO"]
    C --> D["PaymentService.createPayment"]
    D --> E["Generate QR Code<br/>VNPayUtilService"]
    E --> F["Create Payment Entity"]
    F --> G["Save to Database<br/>PaymentRepository"]
    G --> H["Return PaymentResponseDTO"]
    H --> I["Response:<br/>- Payment ID<br/>- QR Code<br/>- Amount<br/>- Status: PENDING"]
```

### 1.2 Callback Handler Flow (Critical)

```mermaid
graph TD
    A["VNPay Returns to Callback URL"] --> B["POST /api/v1/payments/callback"]
    B --> C["PaymentController.handleCallback"]
    C --> D["Extract Callback Data<br/>from Request Parameters"]
    D --> E["VNPayUtilService.verifyCallback<br/>Verify Signature"]
    E --> F{Signature<br/>Valid?}
    F -->|No| G["Return 400 Bad Request<br/>Invalid Signature"]
    F -->|Yes| H["PaymentService.processCallback"]
    H --> I["Parse VNPay Response<br/>- TransactionNo<br/>- ResponseCode<br/>- Amount"]
    I --> J{Response<br/>Success?}
    J -->|No| K["Update Payment Status: FAILED"]
    J -->|Yes| L["Update Payment Status: COMPLETED"]
    K --> M["Log Error"]
    L --> N["Trigger Invoice Creation"]
    M --> O["Return Success Response"]
    N --> O
```

### 1.3 Payment States (Trạng Thái Thanh Toán)

```
┌─────────────────────────────────────────────────┐
│         Payment Status Transitions               │
├─────────────────────────────────────────────────┤
│ PENDING ──> (VNPay Processing)                  │
│      ├──> SUCCESS ──> (Invoice Creation)        │
│      ├──> FAILED ──> (Retry or Cancel)          │
│      └──> EXPIRED ──> (Timeout after 24h)       │
└─────────────────────────────────────────────────┘
```

---

## 📄 Module 2: Invoice Processing

### 2.1 Invoice Creation Flow

```mermaid
graph TD
    A["POST /api/v1/invoices"] --> B["InvoiceController.createInvoice"]
    B --> C["Validate CreateInvoiceDTO"]
    C --> D["InvoiceService.createInvoice"]
    D --> E["Generate Invoice Number<br/>Pattern: INV + Timestamp"]
    E --> F["For each Item:"]
    F --> G["Calculate Amount = Qty × UnitPrice"]
    G --> H["Calculate Tax = Amount × TaxRate%"]
    H --> I["Accumulate Totals"]
    I --> J["Create Invoice Entity:<br/>- invoiceNumber<br/>- customerInfo<br/>- amounts<br/>- status: DRAFT<br/>- jsonData: serialized items"]
    J --> K["Save to Database"]
    K --> L["Return InvoiceResponseDTO"]
```

### 2.2 Invoice State Machine

```
┌────────────────────────────────────────────────────┐
│      Invoice Lifecycle                             │
├────────────────────────────────────────────────────┤
│ DRAFT ──> (POST /issue) ──> ISSUED                │
│           │                   │                    │
│           │                   └──> (POST /generate-pdf)
│           │                        │                │
│           │                        └──> PDF Created │
│           │                              │          │
│           │                              └──> READY_TO_PRINT
│           │                                        │
│           └──> (Can edit items, amounts, etc)      │
│                                                     │
│ ISSUED ──> (Payment confirmed) ──> PAID           │
└────────────────────────────────────────────────────┘
```

### 2.3 PDF Generation Flow

```mermaid
graph TD
    A["POST /api/v1/invoices/{id}/generate-pdf"] --> B["InvoiceController.generatePDF"]
    B --> C["InvoiceService.generateInvoicePDF"]
    C --> D["Retrieve Invoice from Database"]
    D --> E["PDFGenerationService.generatePDF"]
    E --> F["Load Invoice Template"]
    F --> G["Inject Customer & Item Data"]
    G --> H["Apply Formatting & Styling"]
    H --> I["Generate PDF File"]
    I --> J["Save to Storage<br/>Path returned"]
    J --> K["Update Invoice.pdfPath"]
    K --> L["Save Updated Invoice"]
    L --> M["Return PDF Path"]
```

### 2.4 PDF Retrieval Flow

```mermaid
graph TD
    A["GET /api/v1/invoices/{id}/pdf"] --> B["InvoiceController.getPDF"]
    B --> C["InvoiceService.getInvoicePDFBytes"]
    C --> D["Retrieve Invoice"]
    D --> E{PDF Path<br/>Exists?}
    E -->|No| F["Auto-generate PDF<br/>generateInvoicePDF"]
    E -->|Yes| G["Read PDF from Storage"]
    F --> H["Read PDF Bytes<br/>Files.readAllBytes"]
    G --> H
    H --> I["Return PDF Response<br/>Content-Type: application/pdf"]
```

### 2.5 Invoice Data Model

```
Invoice Entity
├── id (PK)
├── invoiceNumber (UNIQUE) - INV1234567890123
├── customerName
├── customerPhone
├── customerEmail
├── customerAddress
├── totalAmount (Tính từ items)
├── taxAmount (Accumulated taxes)
├── grandTotal (totalAmount + taxAmount)
├── status (DRAFT, ISSUED, PAID)
├── createdAt
├── issuedAt (Set when ISSUED)
├── paidAt (Set when PAID)
├── notes
├── pdfPath (File system path)
└── jsonData (Serialized items for history)
```

---

## 🖨️ Module 3: Print Processing

### 3.1 Print Job Submission Flow

```mermaid
graph TD
    A["POST /api/v1/print/submit"] --> B["PrintController.submitPrintJob"]
    B --> C["Validate PrintRequestDTO"]
    C --> D["PrintService.submitPrintJob"]
    D --> E["Get Printer Config:<br/>- Default Printer<br/>- Paper Size<br/>- Queue Settings"]
    E --> F["Create PrintJob Entity:<br/>- documentPath (from Invoice)<br/>- printerName<br/>- copies<br/>- paperSize<br/>- collate<br/>- status: QUEUED<br/>- jobId: generated"]
    F --> G["Save PrintJob to Database"]
    G --> H{Queue<br/>Enabled?}
    H -->|Yes| I["Queue Job<br/>PrinterQueueService.queueJob"]
    H -->|No| J["Direct Print"]
    I --> K["Return PrintResponseDTO"]
    J --> K
```

### 3.2 Print Job State Management

```
┌──────────────────────────────────────────────────┐
│      Print Job Lifecycle                         │
├──────────────────────────────────────────────────┤
│ QUEUED ──> (Printer ready) ──> PRINTING          │
│             │                    │                │
│             │                    └──> COMPLETED   │
│             │                                     │
│             └──> FAILED (Printer error)           │
│                       │                           │
│                       └──> Can RETRY              │
│                                                   │
│ Each job tracks: createdAt, completedAt, errorMsg
└──────────────────────────────────────────────────┘
```

### 3.3 Print Job Status Tracking

```mermaid
graph TD
    A["GET /api/v1/print/jobs/{jobId}"] --> B["PrintController.getPrintJobStatus"]
    B --> C["PrintService.getPrintJobStatus"]
    C --> D["PrintJobRepository.findById"]
    D --> E{Job<br/>Found?}
    E -->|No| F["Throw Exception: Not Found"]
    E -->|Yes| G["Map to PrintResponseDTO:<br/>- jobId<br/>- documentPath<br/>- printerName<br/>- status<br/>- createdAt<br/>- completedAt<br/>- message"]
    G --> H["Return PrintResponseDTO"]
```

### 3.4 PrintJob Data Model

```
PrintJob Entity
├── id (PK)
├── jobId (Generated unique ID)
├── documentPath (from Invoice PDF)
├── printerName (Printer device name)
├── copies (1-100)
├── paperSize (A4, A3, etc)
├── collate (true/false)
├── status (QUEUED, PRINTING, COMPLETED, FAILED)
├── createdAt
├── completedAt
└── errorMessage (if FAILED)
```

---

## 🔗 Inter-Module Integration Flow

### Payment → Invoice Integration

```mermaid
graph TD
    A["VNPay Payment Completed"] --> B["PaymentService.processCallback"]
    B --> C["Update Payment Status: COMPLETED"]
    C --> D["Publish Payment Event<br/>(Optional: Event-driven)"]
    D --> E["Invoice Module<br/>Receives Event OR<br/>API Call from Client"]
    E --> F["POST /api/v1/invoices"]
    F --> G["Create Invoice with<br/>Payment Reference"]
    G --> H["Invoice Ready for<br/>Preview/PDF/Print"]
```

### Invoice → Print Integration

```
┌─────────────────────────────────────────────┐
│  Invoice Workflow → Print Workflow           │
├─────────────────────────────────────────────┤
│ 1. Create Invoice (DRAFT)                   │
│ 2. Issue Invoice (ISSUED)                   │
│ 3. Generate PDF (pdfPath created)           │
│ 4. Client calls Print API with PDF path     │
│ 5. Print Service queues job                 │
│ 6. Print module handles printing            │
│ 7. Return job status to client              │
└─────────────────────────────────────────────┘
```

---

## 📊 Complete System Data Flow Diagram

```mermaid
graph LR
    subgraph Payment["VNPay Module (8081)"]
        P1["PaymentController"]
        P2["PaymentService"]
        P3["Payment DB"]
        P1 --> P2 --> P3
    end
    
    subgraph Invoice["Invoice Module (8082)"]
        I1["InvoiceController"]
        I2["InvoiceService"]
        I3["PDFGenerationService"]
        I4["Invoice DB"]
        I1 --> I2
        I2 --> I3
        I2 --> I4
        I3 --> I4
    end
    
    subgraph Print["Print Module (8083)"]
        PR1["PrintController"]
        PR2["PrintService"]
        PR3["Print Queue"]
        PR4["Print DB"]
        PR1 --> PR2 --> PR3
        PR2 --> PR4
    end
    
    P2 -.->|Invoice Creation| I2
    I2 -.->|PDF Path| PR2
    I3 -.->|PDF Generated| PR2
```

---

## 🎯 API Endpoint Summary

### VNPay Module (`/api/v1/payments`)
- `POST /create` - Tạo request thanh toán
- `GET /{id}` - Lấy thông tin thanh toán
- `POST /callback` - Webhook từ VNPay (CRITICAL)

### Invoice Module (`/api/v1/invoices`)
- `POST /` - Tạo hóa đơn mới (DRAFT)
- `GET /{id}` - Lấy thông tin hóa đơn
- `POST /{id}/issue` - Phát hành hóa đơn
- `POST /{id}/generate-pdf` - Tạo PDF
- `GET /{id}/pdf` - Tải PDF file

### Print Module (`/api/v1/print`)
- `POST /submit` - Gửi công việc in
- `GET /jobs/{jobId}` - Kiểm tra trạng thái in

---

## ⚠️ Key Processing Points (Điểm Quan Trọng)

### Critical Success Factors:

1. **VNPay Callback Verification**
   - ✅ Signature verification bắt buộc
   - ✅ Retry logic for failed payments
   - ✅ Idempotency check (prevent duplicate processing)

2. **Invoice Calculation**
   - ✅ Tax calculation accuracy per item
   - ✅ Grand total = sum of all items + taxes
   - ✅ JSON serialization for audit trail

3. **PDF Generation**
   - ✅ Auto-generate if not exists
   - ✅ Template validation
   - ✅ File storage path management

4. **Print Queue Management**
   - ✅ Job persistence in DB
   - ✅ Queue vs Direct print logic
   - ✅ Error handling for printer failures

---

## 📋 Processing Flow Sequence (Chi Tiết)

### Scenario: Customer buys items and prints invoice

```
Time  | VNPay        | Invoice      | Print        | Database
------|--------------|--------------|--------------|----------
t0    | PENDING      | -            | -            | Payment created
t1    | Customer     | -            | -            | -
      | scans QR     |              |              |
t2    | Processing   | -            | -            | -
t3    | COMPLETED    | -            | -            | Payment updated
t4    | -            | DRAFT        | -            | Invoice created
t5    | -            | ISSUED       | -            | Invoice issued
t6    | -            | Generating   | -            | -
      |              | PDF          |              |
t7    | -            | PDF ready    | -            | Invoice.pdfPath set
t8    | -            | -            | QUEUED       | PrintJob created
t9    | -            | -            | PRINTING     | PrintJob status updated
t10   | -            | -            | COMPLETED    | PrintJob completed
```

---

## 🔍 Error Handling Flows

### Payment Processing Error

```
POST /callback → Invalid Signature → Return 400
                                   → Log error
                                   → No DB update
                                   → Retry from client
```

### Invoice Creation Error

```
POST /invoices → Invalid items
             → Tax calculation overflow
             → Return 400 Bad Request
             → Invoice NOT created
             → Rollback transaction
```

### PDF Generation Error

```
POST /{id}/generate-pdf → Template not found
                        → Disk space exceeded
                        → Return 500 Internal Error
                        → Log stack trace
                        → Invoice pdfPath remains null
```

### Print Job Error

```
POST /submit → Printer offline
            → Queue full
            → Job created with status FAILED
            → Error message in DB
            → Client can retry
```

---

## 📈 Performance Considerations

| Component | Typical Response Time | Bottleneck |
|-----------|----------------------|------------|
| Payment Creation | 100-200ms | QR code generation |
| Invoice Creation | 50-100ms | DB insert |
| PDF Generation | 500-1000ms | Template rendering |
| Print Job Submit | 50-100ms | Queue service |
| Callback Processing | 20-50ms | Signature verification |

---

## ✅ Validation Checkpoints

```mermaid
graph TD
    A["Request Received"] --> B{Content<br/>Validation?}
    B -->|Fail| C["400 Bad Request"]
    B -->|Pass| D{Business<br/>Logic?}
    D -->|Fail| E["400/422<br/>Unprocessable"]
    D -->|Pass| F{Database<br/>Write?}
    F -->|Fail| G["500 Internal Error"]
    F -->|Pass| H["Success Response"]
```

---

## 🎓 Recommended Testing Strategy

1. **Unit Tests**: Service methods with mocked dependencies
2. **Integration Tests**: Service → DB → Repository
3. **API Tests**: Controller → Service flow
4. **Callback Tests**: Mock VNPay responses (success/failure)
5. **End-to-End**: Full payment → invoice → print flow

---

## 📝 Next Implementation Steps

Based on the flow analysis:

1. ✅ Module Setup Complete
2. → Implement VNPay callback verification (most critical)
3. → Implement invoice calculation logic
4. → Implement PDF generation
5. → Implement print queue management
6. → Add comprehensive error handling
7. → Add idempotency checks
8. → Add audit logging
