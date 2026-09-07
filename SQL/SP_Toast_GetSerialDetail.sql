CREATE OR ALTER PROCEDURE SP_Toast_GetSerialDetail
    @serial NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Thong tin FCT / FQC
    SELECT serial_number, work_order, FCT_status, FCT_status_datetime,
           FQC_status, FQC_status_datetime, update_by_svncode
    FROM SVN_Toast_Serial_Info
    WHERE serial_number = @serial;

    -- 2. Thong tin Pallet
    SELECT TOP 1 Id, Date, PackageID, Serial, ScanDate, PalletID, EmployeeID, CountSerial
    FROM SVN_Astro_Label_Data
    WHERE isDeleted = 0 AND EmployeeID = 'toast'
      AND Serial LIKE '%' + @serial + '%'
    ORDER BY Id DESC;

    -- 3. Log san xuat: state = 'Consumed' la tram WIP, state = 'Used' la tram FG
    SELECT id, state, wo_code, master_wo_code, date_finished,
           status, component_list, consumed_wo_code
    FROM SVN_ProductionInputLogs
    WHERE serial_code = @serial
    ORDER BY date_finished;
END
