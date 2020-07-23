function Remove(id, count) {
    if (count > 1) {
        $.ajax({
            type: "GET",
            url: '/Customer/Cart/Minus?cartId=' + id,
            success: function (response) {
                window.location.replace(response.redirectUrl);
            }
        });
    }
    else {
        swal({
            title: "Are you sure you want to Delete?",
            text: "You will not be able to restore the data!",
            icon: "warning",
            buttons: true,
            dangerMode: true
        }).then((willDelete) => {
            if (willDelete) {
                $.ajax({
                    type: "DELETE",
                    url: '/Customer/Cart/Delete?cartId=' + id,
                    success: function (data) {
                        if (data.success) {
                            toastr.success(data.message);
                            window.location.href = '/Customer/Cart/Index'
                        }
                        else {
                            toastr.error(data.message);
                        }
                    }
                });
            }
        });
    }
}

function Delete(id) {
    swal({
        title: "Are you sure you want to Delete?",
        text: "You will not be able to restore the data!",
        icon: "warning",
        buttons: true,
        dangerMode: true
    }).then((willDelete) => {
        if (willDelete) {
            $.ajax({
                type: "DELETE",
                url: '/Customer/Cart/Delete?cartId=' + id,
                success: function (data) {
                    if (data.success) {
                        toastr.success(data.message);
                        window.location.href = '/Customer/Cart/Index'
                    }
                    else {
                        toastr.error(data.message);
                    }
                }
            });
        }
    });
}