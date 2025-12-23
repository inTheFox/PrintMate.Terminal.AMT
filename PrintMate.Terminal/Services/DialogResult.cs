using System;

namespace PrintMate.Terminal.Services;

public class DialogResult<T>
{
    public Guid Id { get; set; }
    public bool IsSuccess = false;
    public T Result { get; set; }
    public void Close() => ModalService.Instance.Close();
}