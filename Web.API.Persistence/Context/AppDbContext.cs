using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;
using Web.API.Domain.Entities;

namespace Web.API.Persistence.Context;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AlarmLogHistory> AlarmLogHistories { get; set; }

    public virtual DbSet<AlarmMaster> AlarmMasters { get; set; }

    public virtual DbSet<Breaktime> Breaktimes { get; set; }

    public virtual DbSet<CardNoMaster> CardNoMasters { get; set; }

    public virtual DbSet<CoatWidthControl> CoatWidthControls { get; set; }

    public virtual DbSet<EmailSender> EmailSenders { get; set; }

    public virtual DbSet<FacilityCount> FacilityCounts { get; set; }

    public virtual DbSet<LineMaster> LineMasters { get; set; }

    public virtual DbSet<LogCycletime> LogCycletimes { get; set; }

    public virtual DbSet<MachineRunHistory> MachineRunHistories { get; set; }

    public virtual DbSet<MachineStatusMaster> MachineStatusMasters { get; set; }

    public virtual DbSet<ModelChangeLog> ModelChangeLogs { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<OaTarget> OaTargets { get; set; }

    public virtual DbSet<ProductMaster> ProductMasters { get; set; }

    public virtual DbSet<ProductionCountHistory> ProductionCountHistories { get; set; }

    public virtual DbSet<ProductionCountMaster> ProductionCountMasters { get; set; }

    public virtual DbSet<ProductionHistory> ProductionHistories { get; set; }

    public virtual DbSet<ProductionPlanMaster> ProductionPlanMasters { get; set; }

    public virtual DbSet<Rolelist> Rolelists { get; set; }

    public virtual DbSet<ShiftMaster> ShiftMasters { get; set; }

    public virtual DbSet<ShiftSchedule> ShiftSchedules { get; set; }

    public virtual DbSet<TelegramToken> TelegramTokens { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<WorkStatusMaster> WorkStatusMasters { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=localhost;database=cataler;user=root;password=root_native;treattinyasboolean=true", Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.36-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<AlarmLogHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("alarm_log_history");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.LineNo).HasColumnName("lineNo");
            entity.Property(e => e.Message)
                .HasMaxLength(200)
                .HasColumnName("message");
            entity.Property(e => e.Timestamp)
                .HasColumnType("timestamp")
                .HasColumnName("timestamp");
        });

        modelBuilder.Entity<AlarmMaster>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("alarm_master");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FailureDetails)
                .HasMaxLength(45)
                .HasColumnName("failureDetails");
            entity.Property(e => e.StatusCode).HasColumnName("statusCode");
        });

        modelBuilder.Entity<Breaktime>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("breaktime");

            entity.Property(e => e.BreakTimeParam)
                .HasColumnType("time")
                .HasColumnName("breakTimeParam");
        });

        modelBuilder.Entity<CardNoMaster>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("card_no_master");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CardNo)
                .HasMaxLength(50)
                .HasColumnName("cardNo");
            entity.Property(e => e.CoatWidthMax)
                .HasMaxLength(50)
                .HasColumnName("coatWidthMax");
            entity.Property(e => e.CoatWidthMin)
                .HasMaxLength(50)
                .HasColumnName("coatWidthMin");
            entity.Property(e => e.CoatWidthTarget)
                .HasMaxLength(50)
                .HasColumnName("coatWidthTarget");
            entity.Property(e => e.IsDeleted).HasColumnName("isDeleted");
            entity.Property(e => e.LineName)
                .HasMaxLength(100)
                .HasColumnName("lineName");
            entity.Property(e => e.LineNo)
                .HasMaxLength(50)
                .HasColumnName("lineNo");
            entity.Property(e => e.MaterialName)
                .HasMaxLength(100)
                .HasColumnName("materialName");
            entity.Property(e => e.MaterialNo)
                .HasMaxLength(100)
                .HasColumnName("materialNo");
            entity.Property(e => e.PHmax)
                .HasMaxLength(50)
                .HasColumnName("pHMax");
            entity.Property(e => e.PHmin)
                .HasMaxLength(50)
                .HasColumnName("pHMin");
            entity.Property(e => e.PartNo)
                .HasMaxLength(100)
                .HasColumnName("partNo");
            entity.Property(e => e.PassHour)
                .HasMaxLength(50)
                .HasColumnName("passHour");
            entity.Property(e => e.ProductName)
                .HasMaxLength(100)
                .HasColumnName("productName");
            entity.Property(e => e.SolidityMax)
                .HasMaxLength(50)
                .HasColumnName("solidityMax");
            entity.Property(e => e.SolidityMin)
                .HasMaxLength(50)
                .HasColumnName("solidityMin");
            entity.Property(e => e.SolidityTarget)
                .HasMaxLength(50)
                .HasColumnName("solidityTarget");
            entity.Property(e => e.SubstrateName)
                .HasMaxLength(100)
                .HasColumnName("substrateName");
            entity.Property(e => e.TactTime)
                .HasMaxLength(50)
                .HasColumnName("tactTime");
            entity.Property(e => e.Viscosity100Max)
                .HasMaxLength(50)
                .HasColumnName("viscosity100Max");
            entity.Property(e => e.Viscosity100Min)
                .HasMaxLength(50)
                .HasColumnName("viscosity100Min");
            entity.Property(e => e.Viscosity1Max)
                .HasMaxLength(50)
                .HasColumnName("viscosity1Max");
            entity.Property(e => e.Viscosity1Min)
                .HasMaxLength(50)
                .HasColumnName("viscosity1Min");
        });

        modelBuilder.Entity<CoatWidthControl>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("coat_width_control");

            entity.HasIndex(e => e.LineMasterId, "fk_coat_line");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Bcd4digit).HasColumnName("bcd4digit");
            entity.Property(e => e.CoatWidthAvg).HasColumnName("coatWidthAvg");
            entity.Property(e => e.CoatingNo).HasColumnName("coatingNo");
            entity.Property(e => e.CoatingPressureKpa).HasColumnName("coatingPressureKpa");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("createdAt");
            entity.Property(e => e.Emisi).HasColumnName("emisi");
            entity.Property(e => e.KpaAccuracy).HasColumnName("kpaAccuracy");
            entity.Property(e => e.KpaRecommend).HasColumnName("kpaRecommend");
            entity.Property(e => e.LineMasterId).HasColumnName("lineMasterId");
            entity.Property(e => e.ProdMemberId).HasColumnName("prodMemberId");
            entity.Property(e => e.ProdStaffId).HasColumnName("prodStaffId");
            entity.Property(e => e.RecordDate).HasColumnName("recordDate");
            entity.Property(e => e.Remark)
                .HasMaxLength(50)
                .HasColumnName("remark");
            entity.Property(e => e.Solidity)
                .HasPrecision(5, 2)
                .HasColumnName("solidity");
            entity.Property(e => e.SubProductName)
                .HasMaxLength(45)
                .HasColumnName("subProductName");
            entity.Property(e => e.Vis100rpm).HasColumnName("vis100rpm");
            entity.Property(e => e.Vis1rpm).HasColumnName("vis1rpm");

            entity.HasOne(d => d.LineMaster).WithMany(p => p.CoatWidthControls)
                .HasForeignKey(d => d.LineMasterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_coat_line");
        });

        modelBuilder.Entity<EmailSender>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("email_sender");

            entity.Property(e => e.Email)
                .HasMaxLength(45)
                .HasColumnName("email");
        });

        modelBuilder.Entity<FacilityCount>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("facility_count");

            entity.HasIndex(e => e.LineMasterId, "fk_facility_line");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .HasColumnName("category");
            entity.Property(e => e.CollectDate)
                .HasColumnType("datetime")
                .HasColumnName("collectDate");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("createdAt");
            entity.Property(e => e.Device)
                .HasMaxLength(50)
                .HasColumnName("device");
            entity.Property(e => e.LimitValue).HasColumnName("limitValue");
            entity.Property(e => e.LineMasterId).HasColumnName("lineMasterId");
            entity.Property(e => e.Result).HasColumnName("result");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("updatedAt");

            entity.HasOne(d => d.LineMaster).WithMany(p => p.FacilityCounts)
                .HasForeignKey(d => d.LineMasterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_facility_line");
        });

        modelBuilder.Entity<LineMaster>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("line_master");

            entity.HasIndex(e => e.LineNo, "ix_lm_lineNo").IsUnique();

            entity.HasIndex(e => e.LineName, "machine_name_UNIQUE").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.LineName)
                .HasMaxLength(20)
                .HasColumnName("lineName");
            entity.Property(e => e.LineNo).HasColumnName("lineNo");
        });

        modelBuilder.Entity<LogCycletime>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("log_cycletime");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ACoat1).HasColumnName("aCoat1");
            entity.Property(e => e.ACoat2).HasColumnName("aCoat2");
            entity.Property(e => e.ACoat3).HasColumnName("aCoat3");
            entity.Property(e => e.BCoat1).HasColumnName("bCoat1");
            entity.Property(e => e.BCoat2).HasColumnName("bCoat2");
            entity.Property(e => e.BCoat3).HasColumnName("bCoat3");
            entity.Property(e => e.GatewayTimestamp)
                .HasColumnType("datetime")
                .HasColumnName("gatewayTimestamp");
            entity.Property(e => e.Judgement)
                .HasMaxLength(20)
                .HasColumnName("judgement");
            entity.Property(e => e.MachineId).HasColumnName("machineId");
            entity.Property(e => e.Result)
                .HasPrecision(10, 2)
                .HasColumnName("result");
            entity.Property(e => e.Target)
                .HasPrecision(10, 2)
                .HasColumnName("target");
            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("timestamp");
        });

        modelBuilder.Entity<MachineRunHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("machine_run_history");

            entity.HasIndex(e => e.DateStart, "ix_mrh_datestart");

            entity.HasIndex(e => new { e.LineNo, e.DateEnd }, "ix_mrh_line_active");

            entity.HasIndex(e => new { e.LineNo, e.DateStart, e.DateEnd }, "ix_mrh_line_dates");

            entity.HasIndex(e => new { e.LineNo, e.Status, e.DateEnd }, "ix_mrh_line_status_active");

            entity.HasIndex(e => new { e.LineNo, e.Status, e.DateStart }, "ix_mrh_line_status_date");

            entity.Property(e => e.DateEnd)
                .HasColumnType("datetime")
                .HasColumnName("dateEnd");
            entity.Property(e => e.DateStart)
                .HasColumnType("datetime")
                .HasColumnName("dateStart");
            entity.Property(e => e.LineNo).HasColumnName("lineNo");
            entity.Property(e => e.Status)
                .HasMaxLength(25)
                .HasColumnName("status");
        });

        modelBuilder.Entity<MachineStatusMaster>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("machine_status_master");

            entity.HasIndex(e => e.StatusCode, "status_code").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StatusCode).HasColumnName("statusCode");
            entity.Property(e => e.StatusColor)
                .HasMaxLength(50)
                .HasColumnName("statusColor");
            entity.Property(e => e.StatusLabel)
                .HasMaxLength(50)
                .HasColumnName("statusLabel");
        });

        modelBuilder.Entity<ModelChangeLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("model_change_log")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => new { e.LineNo, e.StartRunTime }, "ix_model_change_line_time");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.LineNo).HasColumnName("lineNo");
            entity.Property(e => e.ModelName)
                .HasMaxLength(200)
                .HasColumnName("modelName");
            entity.Property(e => e.StartRunTime)
                .HasColumnType("datetime")
                .HasColumnName("startRunTime");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("notification");

            entity.Property(e => e.ChangeOver).HasColumnName("changeOver");
            entity.Property(e => e.FacilityCount).HasColumnName("facilityCount");
            entity.Property(e => e.Name)
                .HasMaxLength(45)
                .HasColumnName("name");
            entity.Property(e => e.Problems).HasColumnName("problems");
            entity.Property(e => e.Target)
                .HasMaxLength(45)
                .HasColumnName("target");
            entity.Property(e => e.Type).HasColumnName("type");
        });

        modelBuilder.Entity<OaTarget>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("oa_target");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.LineNo).HasColumnName("lineNo");
            entity.Property(e => e.Target).HasColumnName("target");
        });

        modelBuilder.Entity<ProductMaster>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("product_master");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.LineNo).HasColumnName("lineNo");
            entity.Property(e => e.ProductName)
                .HasMaxLength(45)
                .HasColumnName("productName");
        });

        modelBuilder.Entity<ProductionCountHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("production_count_history")
                .UseCollation("utf8mb4_unicode_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Actual).HasColumnName("actual");
            entity.Property(e => e.CardNo).HasColumnName("cardNo");
            entity.Property(e => e.LineMasterId).HasColumnName("lineMasterId");
            entity.Property(e => e.Target).HasColumnName("target");
            entity.Property(e => e.Timestamp)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("timestamp");
        });

        modelBuilder.Entity<ProductionCountMaster>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("production_count_master")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => new { e.LineMasterId, e.OperationHour }, "ix_pcm_line_hour");

            entity.HasIndex(e => new { e.LineMasterId, e.DataDate }, "ix_pcm_line_time");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DataDate)
                .HasColumnType("time")
                .HasColumnName("dataDate");
            entity.Property(e => e.DispOrder)
                .HasMaxLength(2)
                .IsFixedLength()
                .HasColumnName("dispOrder");
            entity.Property(e => e.LineMasterId).HasColumnName("lineMasterId");
            entity.Property(e => e.OperationHour).HasColumnName("operationHour");
            entity.Property(e => e.PathControl).HasColumnName("pathControl");
            entity.Property(e => e.Target).HasColumnName("target");
        });

        modelBuilder.Entity<ProductionHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("production_history")
                .UseCollation("utf8mb4_unicode_ci");

            entity.HasIndex(e => e.ProductionPlanId, "idx_productionPlanId").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ActualQty).HasColumnName("actualQty");
            entity.Property(e => e.ProductionPlanId).HasColumnName("productionPlanId");
            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("timestamp");

            entity.HasOne(d => d.ProductionPlan).WithOne(p => p.ProductionHistory)
                .HasForeignKey<ProductionHistory>(d => d.ProductionPlanId)
                .HasConstraintName("fk_production_history_plan");
        });

        modelBuilder.Entity<ProductionPlanMaster>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("production_plan_master");

            entity.HasIndex(e => new { e.PlanDate, e.LineMasterId }, "ix_ppm_planDate_lineMasterId");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp")
                .HasColumnName("createdAt");
            entity.Property(e => e.DateFile).HasColumnName("dateFile");
            entity.Property(e => e.FileName).HasColumnName("fileName");
            entity.Property(e => e.LineMasterId).HasColumnName("lineMasterId");
            entity.Property(e => e.PlanDate).HasColumnName("planDate");
            entity.Property(e => e.PlanQty).HasColumnName("planQty");
            entity.Property(e => e.ProductMasterId).HasColumnName("productMasterId");
            entity.Property(e => e.WorkStatusMasterId).HasColumnName("work_status_masterId");
        });

        modelBuilder.Entity<Rolelist>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("rolelist");

            entity.HasIndex(e => e.Role, "role_UNIQUE").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Role)
                .HasMaxLength(45)
                .HasColumnName("role");
        });

        modelBuilder.Entity<ShiftMaster>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("shift_master");

            entity.HasIndex(e => e.Code, "code").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(10)
                .HasColumnName("code");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<ShiftSchedule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("shift_schedule");

            entity.HasIndex(e => new { e.ScheduleType, e.StartTime, e.EndTime }, "ix_shift_schedule_type_time");

            entity.HasIndex(e => new { e.ShiftId, e.ScheduleType }, "uq_shift_schedule").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CrossesMidnight).HasColumnName("crossesMidnight");
            entity.Property(e => e.EndTime)
                .HasColumnType("time")
                .HasColumnName("endTime");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("isActive");
            entity.Property(e => e.ScheduleType)
                .HasColumnType("enum('NORMAL','RAMADAN')")
                .HasColumnName("scheduleType");
            entity.Property(e => e.ShiftId).HasColumnName("shiftId");
            entity.Property(e => e.StartTime)
                .HasColumnType("time")
                .HasColumnName("startTime");

            entity.HasOne(d => d.Shift).WithMany(p => p.ShiftSchedules)
                .HasForeignKey(d => d.ShiftId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_shift_schedule_shift");
        });

        modelBuilder.Entity<TelegramToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("telegram_token");

            entity.Property(e => e.Id).HasMaxLength(45);
            entity.Property(e => e.Token).HasColumnName("token");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("users");

            entity.HasIndex(e => e.UserId, "userId").IsUnique();

            entity.HasIndex(e => e.UserName, "userName").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("createdAt");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("isActive");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("passwordHash");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .HasColumnName("phoneNumber");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasDefaultValueSql("'user'")
                .HasColumnName("role");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("updatedAt");
            entity.Property(e => e.UserId)
                .HasMaxLength(20)
                .HasColumnName("userId");
            entity.Property(e => e.UserName)
                .HasMaxLength(50)
                .HasColumnName("userName");
        });

        modelBuilder.Entity<WorkStatusMaster>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("work_status_master");

            entity.Property(e => e.StatusCode).HasColumnName("statusCode");
            entity.Property(e => e.StatusLabel)
                .HasMaxLength(45)
                .HasColumnName("statusLabel");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
