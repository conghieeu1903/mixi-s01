#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// Lớp gốc của một kịch bản test tự động chơi thử game, mỗi file trong Tests/Cases là một kịch bản
public abstract class GameTestCase
{
    // Mã test case, dùng để sắp thứ tự chạy (TC01, TC02...)
    public abstract string Id { get; }

    // Tên ngắn gọn của kịch bản
    public abstract string Title { get; }

    // Mô tả các bước và kết quả mong đợi
    public virtual string Description => "";

    // Thời gian tối đa (giây thực) cho cả kịch bản
    public virtual float Timeout => 60f;

    // Tên hiển thị gồm mã và tiêu đề
    public string DisplayName => $"{Id} - {Title}";

    // Các bước chơi thử và kiểm tra kết quả
    public abstract IEnumerator Run(TestContext t);

    // Tìm mọi test case trong project và sắp theo Id
    public static List<GameTestCase> DiscoverAll()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return Type.EmptyTypes; }
            })
            .Where(t => typeof(GameTestCase).IsAssignableFrom(t) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null)
            .Select(t => (GameTestCase)Activator.CreateInstance(t))
            .OrderBy(c => c.Id, StringComparer.Ordinal)
            .ToList();
    }
}

// Lỗi báo test case không đạt
public class TestFailure : Exception
{
    // Tạo lỗi kèm thông báo
    public TestFailure(string message) : base(message) { }
}

// Báo test case được bỏ qua
public class TestSkipped : Exception
{
    // Tạo thông báo bỏ qua kèm lý do
    public TestSkipped(string message) : base(message) { }
}
#endif
