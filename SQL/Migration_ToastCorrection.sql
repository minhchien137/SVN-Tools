-- Migration cho chuc nang "Sua du lieu Toast": sua linh kien sai + doi SN
-- Khong dong vao cac bang san xuat hien co, chi tao them 1 bang audit log rieng.
-- Chay 1 lan tren SQL Server truoc khi dung chuc nang nay.

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SVN_Toast_Edit_Log')
BEGIN
    CREATE TABLE dbo.SVN_Toast_Edit_Log (
        Id            INT IDENTITY(1,1) PRIMARY KEY,
        ActionType    NVARCHAR(30)   NOT NULL,  -- 'ReplaceComponent' | 'RenameSerial'
        SerialCode    NVARCHAR(100)  NOT NULL,  -- SN dang duoc sua (ReplaceComponent) / SN cu (RenameSerial)
        RelatedSerial NVARCHAR(100)  NULL,      -- SN moi (chi dung cho RenameSerial)
        Station       NVARCHAR(20)   NULL,      -- 'WIP' | 'FG' (chi dung cho ReplaceComponent)
        OldValue      NVARCHAR(MAX)  NULL,
        NewValue      NVARCHAR(MAX)  NULL,
        Reason        NVARCHAR(500)  NULL,
        EditedBy      NVARCHAR(100)  NULL,
        EditedAt      DATETIME       NOT NULL DEFAULT GETDATE()
    );
END
