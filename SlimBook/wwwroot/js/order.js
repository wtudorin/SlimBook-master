var dataTable;
$(document).ready(function () {
	var url = window.location.search;
	if (url.includes("inprocess")) {
		loadDataTable("GetOrderList?status=inprocess");
		return;
	}
	if (url.includes("pending")) {
		loadDataTable("GetOrderList?status=pending");
		return;
	}
	if (url.includes("complete")) {
		loadDataTable("GetOrderList?status=complete");
		return;
	}
	if (url.includes("rejected")) {
		loadDataTable("GetOrderList?status=rejected");
		return;
	}
	loadDataTable("GetOrderList?status=all");
});

function loadDataTable(url) {
	dataTable = $("#tblData").DataTable({
		"ajax": {
			"url": "/Admin/Order/" + url
		},
		"columns": [
			{ "data": "id", "width": "10%" },																
			{ "data": "name", "width": "15%" },
			{ "data": "phoneNumber", "width": "15%" },
			{ "data": "applicationUser.email", "width": "15%" },
			{ "data": "orderStatus", "width": "15%" },
			{ "data": "orderTotal", "width": "15%" },
			{
				"data": "id",
				"render": function (data) {
					return `<div class="text-center">
                                    <a href="/Admin/Order/Details/${data}" class="btn btn-success text-white" style="cursor:pointer">
                                        <i class="fas fa-edit"></i>
                                    </a>
                                </div>
                        `;
				},
				"width": "5%"
			}
		]
	});
}