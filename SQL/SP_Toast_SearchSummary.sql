CREATE OR ALTER PROCEDURE SP_Toast_SearchSummary
    @q NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    -- Tim tat ca serial khop voi bat ky truong nao trong 3 bang Toast
    WITH MatchedSerials AS (
        -- Tu SVN_Toast_Serial_Info
        SELECT serial_number AS sn
        FROM SVN_Toast_Serial_Info
        WHERE serial_number                LIKE '%' + @q + '%'
           OR ISNULL(work_order,'')        LIKE '%' + @q + '%'
           OR ISNULL(FCT_status,'')        LIKE '%' + @q + '%'
           OR ISNULL(FQC_status,'')        LIKE '%' + @q + '%'
           OR ISNULL(update_by_svncode,'') LIKE '%' + @q + '%'

        UNION

        -- Tu SVN_Astro_Label_Data (Serial la danh sach phan cach dau phay)
        SELECT LTRIM(RTRIM(value)) AS sn
        FROM SVN_Astro_Label_Data
        CROSS APPLY STRING_SPLIT(Serial, ',')
        WHERE isDeleted = 0 AND EmployeeID = 'toast'
          AND (PalletID  LIKE '%' + @q + '%'
            OR PackageID LIKE '%' + @q + '%'
            OR Date      LIKE '%' + @q + '%'
            OR Serial    LIKE '%' + @q + '%')

        UNION

        -- Tu SVN_ProductionInputLogs
        SELECT serial_code AS sn
        FROM SVN_ProductionInputLogs
        WHERE serial_code    LIKE '%' + @q + '%'
           OR wo_code        LIKE '%' + @q + '%'
           OR master_wo_code LIKE '%' + @q + '%'
    )
    SELECT DISTINCT
        m.sn                    AS serial_number,
        t.FCT_status,
        t.FQC_status,
        t.FCT_status_datetime,
        t.FQC_status_datetime,
        t.work_order,
        a.PalletID
    FROM MatchedSerials m
    LEFT JOIN SVN_Toast_Serial_Info t ON t.serial_number = m.sn
    LEFT JOIN SVN_Astro_Label_Data a
           ON a.isDeleted = 0 AND a.EmployeeID = 'toast'
          AND a.Serial LIKE '%' + m.sn + '%'
    WHERE LTRIM(RTRIM(m.sn)) != '';
END
