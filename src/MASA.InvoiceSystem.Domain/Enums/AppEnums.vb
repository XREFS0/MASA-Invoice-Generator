Namespace Enums
    Public Enum InvoiceStatus
        Draft = 0
        Sent = 1
        Paid = 2
        PartialPayment = 3
        Overdue = 4
        Cancelled = 5
    End Enum

    Public Enum PaymentMethod
        Cash = 0
        BankTransfer = 1
        CreditCard = 2
        DebitCard = 3
        PayPal = 4
        Other = 5
    End Enum

    Public Enum ProductType
        Product = 0
        Service = 1
    End Enum

    Public Enum AuditAction
        Created = 0
        Updated = 1
        Deleted = 2
        StatusChanged = 3
        PaymentRecorded = 4
        DatabaseRestored = 5
        DatabaseBackedUp = 6
    End Enum
End Namespace
