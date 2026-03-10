using System.Runtime.InteropServices;
using Vanara.PInvoke;

namespace Fischless.WindowsInput;

internal class WindowsInputMessageDispatcher : IInputMessageDispatcher
{
    public void DispatchInput(User32.INPUT[] inputs)
    {
        if (inputs == null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        if (inputs.Length == 0)
        {
            throw new ArgumentException("The input array was empty", nameof(inputs));
        }

        uint num = User32.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(User32.INPUT)));

        if (num != (ulong)(long)inputs.Length)
        {
            throw new Exception("Gửi tín hiệu phím chuột giả lập thất bại! Nguyên nhân thường gặp: 1. Bạn chưa chạy chương trình với quyền Admin; 2. Bị phần mềm diệt virus chặn (ví dụ 360/Avast...)");
        }
    }
}
