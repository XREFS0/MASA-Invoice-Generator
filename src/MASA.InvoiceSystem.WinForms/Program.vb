Imports System
Imports System.Windows.Forms
Imports MASA.InvoiceSystem.Application.Interfaces
Imports MASA.InvoiceSystem.Application.Services
Imports MASA.InvoiceSystem.Domain.Interfaces
Imports MASA.InvoiceSystem.Infrastructure.Data
Imports MASA.InvoiceSystem.Infrastructure.Repositories
Imports MASA.InvoiceSystem.Infrastructure.Services
Imports MASA.InvoiceSystem.WinForms.Forms
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Logging

Namespace MASA.InvoiceSystem.WinForms
    Friend Module Program
        Private _serviceProvider As IServiceProvider

        <STAThread()>
        Friend Sub Main()
            System.Windows.Forms.Application.SetHighDpiMode(HighDpiMode.SystemAware)
            System.Windows.Forms.Application.EnableVisualStyles()
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(False)

            Dim services As New ServiceCollection()
            ConfigureServices(services)
            _serviceProvider = services.BuildServiceProvider()

            AddHandler System.Windows.Forms.Application.ThreadException, AddressOf OnThreadException
            AddHandler AppDomain.CurrentDomain.UnhandledException, AddressOf OnUnhandledException

            Try
                Dim dbInitializer = _serviceProvider.GetRequiredService(Of IDatabaseInitializer)()
                dbInitializer.InitializeAsync().GetAwaiter().GetResult()
            Catch ex As Exception
                Dim logger = _serviceProvider.GetService(Of ILogger(Of IDatabaseInitializer))()
                logger?.LogCritical(ex, "Fatal error during SQLite database initialization.")
                MessageBox.Show($"Failed to initialize local SQLite database:{Environment.NewLine}{ex.Message}", "Database Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End Try

            Dim mainForm = _serviceProvider.GetRequiredService(Of MainForm)()
            System.Windows.Forms.Application.Run(mainForm)
        End Sub

        Private Sub ConfigureServices(services As IServiceCollection)
            services.AddLogging(Sub(builder)
                                    builder.AddConsole()
                                    builder.AddDebug()
                                    builder.SetMinimumLevel(LogLevel.Information)
                                End Sub)

            Dim dbConfig As New DatabaseConfig()
            services.AddSingleton(dbConfig)
            services.AddSingleton(Of ISqliteConnectionFactory, SqliteConnectionFactory)()
            services.AddTransient(Of IDatabaseInitializer, DatabaseInitializer)()

            services.AddTransient(Of ICustomerRepository, CustomerRepository)()
            services.AddTransient(Of IProductRepository, ProductRepository)()
            services.AddTransient(Of IInvoiceRepository, InvoiceRepository)()
            services.AddTransient(Of IPaymentRepository, PaymentRepository)()
            services.AddTransient(Of ICompanySettingRepository, CompanySettingRepository)()
            services.AddTransient(Of ITaxRateRepository, TaxRateRepository)()
            services.AddTransient(Of IAuditLogRepository, AuditLogRepository)()

            services.AddTransient(Of IInvoicePdfService, QuestPdfInvoiceGenerator)()
            services.AddTransient(Of IBackupService, BackupService)()

            services.AddSingleton(Of IInvoiceCalculationService, InvoiceCalculationService)()
            services.AddTransient(Of ICustomerService, CustomerService)()
            services.AddTransient(Of IProductService, ProductService)()
            services.AddTransient(Of IInvoiceService, InvoiceService)()
            services.AddTransient(Of IPaymentService, PaymentService)()
            services.AddTransient(Of ICompanySettingService, CompanySettingService)()
            services.AddTransient(Of ITaxRateService, TaxRateService)()
            services.AddTransient(Of IDashboardService, DashboardService)()
            services.AddTransient(Of IReportService, ReportService)()
            services.AddTransient(Of IAuditLogService, AuditLogService)()

            services.AddTransient(Of MainForm)()
        End Sub

        Private Sub OnThreadException(sender As Object, e As Threading.ThreadExceptionEventArgs)
            Dim logger = _serviceProvider?.GetService(Of ILogger(Of MainForm))()
            logger?.LogError(e.Exception, "Unhandled UI thread exception occurred.")
            MessageBox.Show($"An unexpected error occurred:{Environment.NewLine}{e.Exception.Message}", "Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Sub

        Private Sub OnUnhandledException(sender As Object, e As UnhandledExceptionEventArgs)
            Dim ex = TryCast(e.ExceptionObject, Exception)
            Dim logger = _serviceProvider?.GetService(Of ILogger(Of MainForm))()
            logger?.LogCritical(ex, "Fatal domain unhandled exception occurred.")
            MessageBox.Show($"A critical error occurred:{Environment.NewLine}{If(ex IsNot Nothing, ex.Message, "Unknown fatal error.")}", "Fatal Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Sub
    End Module
End Namespace
