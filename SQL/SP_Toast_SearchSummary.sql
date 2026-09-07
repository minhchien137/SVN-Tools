CREATE OR ALTER PROCEDURE SP_Toast_SearchSummary
    @q NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    -- Tim tat ca serial khop, co gan nhan loai truong da khop (match_type) de hien thi cho nguoi dung
    WITH MatchedSerials AS (
        SELECT serial_number AS sn, N'Serial' AS match_type
        FROM SVN_Toast_Serial_Info
        WHERE serial_number LIKE '%' + @q + '%'

        UNION ALL

        SELECT serial_number AS sn, N'Work Order' AS match_type
        FROM SVN_Toast_Serial_Info
        WHERE ISNULL(work_order,'') LIKE '%' + @q + '%'

        UNION ALL

        SELECT serial_number AS sn, N'Trạng thái' AS match_type
        FROM SVN_Toast_Serial_Info
        WHERE ISNULL(FCT_status,'') LIKE '%' + @q + '%'
           OR ISNULL(FQC_status,'') LIKE '%' + @q + '%'

        UNION ALL

        SELECT serial_number AS sn, N'Nhân viên' AS match_type
        FROM SVN_Toast_Serial_Info
        WHERE ISNULL(update_by_svncode,'') LIKE '%' + @q + '%'

        UNION ALL

        -- Tu SVN_Astro_Label_Data (Serial la danh sach phan cach dau phay)
        SELECT LTRIM(RTRIM(value)) AS sn, N'Pallet' AS match_type
        FROM SVN_Astro_Label_Data
        CROSS APPLY STRING_SPLIT(Serial, ',')
        WHERE isDeleted = 0 AND EmployeeID = 'toast'
          AND (PalletID LIKE '%' + @q + '%' OR PackageID LIKE '%' + @q + '%' OR Date LIKE '%' + @q + '%')

        UNION ALL

        SELECT LTRIM(RTRIM(value)) AS sn, N'Serial' AS match_type
        FROM SVN_Astro_Label_Data
        CROSS APPLY STRING_SPLIT(Serial, ',')
        WHERE isDeleted = 0 AND EmployeeID = 'toast'
          AND Serial LIKE '%' + @q + '%'

        UNION ALL

        -- Tu SVN_ProductionInputLogs (gan them ten tram theo state: Consumed = WIP, Used = FG)
        SELECT serial_code AS sn, N'Serial' AS match_type
        FROM SVN_ProductionInputLogs
        WHERE serial_code LIKE '%' + @q + '%'

        UNION ALL

        SELECT serial_code AS sn,
            N'Work Order (' + CASE state WHEN 'Consumed' THEN N'Trạm WIP' WHEN 'Used' THEN N'Trạm FG' ELSE N'Sản xuất' END + N')' AS match_type
        FROM SVN_ProductionInputLogs
        WHERE wo_code LIKE '%' + @q + '%' OR master_wo_code LIKE '%' + @q + '%'

        UNION ALL

        SELECT serial_code AS sn,
            N'Lot (' + CASE state WHEN 'Consumed' THEN N'Trạm WIP' WHEN 'Used' THEN N'Trạm FG' ELSE N'Sản xuất' END + N')' AS match_type
        FROM SVN_ProductionInputLogs
        WHERE ISNULL(component_list,'') LIKE '%' + @q + '%'
    ),
    DistinctSerials AS (
        SELECT DISTINCT sn FROM MatchedSerials WHERE LTRIM(RTRIM(sn)) != ''
    ),
    MatchTypeAgg AS (
        SELECT sn, STRING_AGG(match_type, ', ') WITHIN GROUP (ORDER BY match_type) AS match_types
        FROM (SELECT DISTINCT sn, match_type FROM MatchedSerials) x
        GROUP BY sn
    ),
    -- Gom cac component_list (chua lot number) cua tung serial, dung de hien thi/loc nhanh
    SerialLots AS (
        SELECT serial_code AS sn, STRING_AGG(component_list, ' | ') AS lots_raw
        FROM SVN_ProductionInputLogs
        WHERE component_list IS NOT NULL
        GROUP BY serial_code
    )
    SELECT DISTINCT
        m.sn                    AS serial_number,
        t.FCT_status,
        t.FQC_status,
        t.FCT_status_datetime,
        t.FQC_status_datetime,
        t.work_order,
        a.PalletID,
        sl.lots_raw,
        mt.match_types
    FROM DistinctSerials m
    LEFT JOIN SVN_Toast_Serial_Info t ON t.serial_number = m.sn
    LEFT JOIN SVN_Astro_Label_Data a
           ON a.isDeleted = 0 AND a.EmployeeID = 'toast'
          AND a.Serial LIKE '%' + m.sn + '%'
    LEFT JOIN SerialLots sl ON sl.sn = m.sn
    LEFT JOIN MatchTypeAgg mt ON mt.sn = m.sn;
END
