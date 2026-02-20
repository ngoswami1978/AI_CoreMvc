var gridTable;
var csrfToken = $('input[name="__RequestVerificationToken"]').val();

$(document).ready(function () {
    InitDropdowns();
    LoadGrid();
    BindColumnSearch();

    $(document).on('change', '#parentId', function () {
        LoadChildByParent($(this).val(), '#childId');
    });

    $(document).on('change', '#childId', function () {
        var extra = $(this).find('option:selected').attr('data-country') || '';
        $('#autoFilledField').val(extra);
    });
});

function InitDropdowns() {
    $('.select2').select2({ placeholder: 'Select', allowClear: true, width: '100%' });
    $('.select2-modal').select2({ dropdownParent: $('#myModal'), placeholder: 'Select', allowClear: true, width: '100%' });
    PopulateSelect('#parentId', dropdownData.groupCompany);
    PopulateSelect('#childId', dropdownData.plant);
    PopulateSelect('#supplierId', dropdownData.supplier);
    PopulateSelect('#currencyId', dropdownData.currency);
}

function PopulateSelect(selector, items) {
    var $sel = $(selector);
    items.forEach(function (item) {
        if ($sel.find('option[value="' + item.value + '"]').length === 0) {
            $sel.append(new Option(item.text, item.value));
        }
    });
}

function LoadChildByParent(parentId, targetSelector) {
    $.get('/TableName/GetPlants', { groupCompanyId: parentId || '' }, function (data) {
        var $target = $(targetSelector).empty().append('<option value="">Select</option>');
        data.forEach(function (item) {
            $target.append($('<option>', { value: item.value, text: item.text }).attr('data-country', item.country || ''));
        });
        $target.trigger('change.select2');
    });
}

function LoadGrid(filters) {
    if ($.fn.DataTable.isDataTable('#gridTable')) {
        $('#gridTable').DataTable().destroy();
        $('#gridTable tbody').empty();
    }
    gridTable = $('#gridTable').DataTable({
        processing: true,
        ajax: {
            url: '/TableName/GetAll',
            type: 'GET',
            data: filters || {},
            dataSrc: function (json) { return json.success ? json.data : []; },
            error: function () { ShowToast('Failed to load data.', 'error'); }
        },
        columns: [
            { data: 'recordId', render: function (d) { return '<a href="#" onclick="EditRecord(' + d + ')">Edit</a> | <a href="#" onclick="DeleteRecord(' + d + ')">Hist</a>'; } },
            { data: 'fieldOne' }, { data: 'groupCompanyName' }, { data: 'plantName' }, { data: 'plantCountry' },
            { data: 'supplierName' }, { data: 'currencyCode' },
            { data: 'startDate', render: function (d) { return FormatDate(d); } },
            { data: 'endDate', render: function (d) { return FormatDate(d); } },
            { data: 'amount' }
        ],
        pageLength: 10,
        lengthMenu: [10, 25, 50, 100],
        dom: 'rtip',
        scrollX: true,
        language: {
            info: '_START_ - _END_ of _TOTAL_ records',
            paginate: { first: '|◄', previous: '◄ Prev', next: 'Next ►', last: '►|' }
        }
    });
}

function BindColumnSearch() {
    $('#gridTable .col-search').each(function (i) {
        $(this).off('keyup').on('keyup', function () { gridTable.column(i + 1).search(this.value).draw(); });
    });
}

function SearchRecords() {
    var filters = { groupCompanyId: $('#filterGroupCompany').val() || null, plantId: $('#filterPlant').val() || null, fieldOne: $('#filterFieldOne').val() || null };
    LoadGrid(filters);
}

function ClearFilters() {
    $('.filter-select').val(null).trigger('change');
    $('#filterFieldOne').val('');
    LoadGrid();
}

function OpenAddModal() { ResetForm(); $('#modalTitleText').text('Add New'); $('#myModal').modal('show'); }

function EditRecord(id) {
    ShowLoader(true);
    $.get('/TableName/GetById', { id: id }, function (res) {
        ShowLoader(false);
        if (!res.success) { ShowToast(res.message, 'error'); return; }
        var d = res.data; ResetForm(); $('#modalTitleText').text('Edit Record');
        $('#recordId').val(d.recordId); $('#fieldOne').val(d.fieldOne); $('#fieldTwo').val(d.fieldTwo);
        $('#supplierId').val(d.supplierId).trigger('change.select2');
        $('#currencyId').val(d.currencyId).trigger('change.select2');
        $('#startDate').val(FormatDateInput(d.startDate)); $('#endDate').val(FormatDateInput(d.endDate));
        LoadChildByParent(d.groupCompanyId, '#childId');
        setTimeout(function () {
            SetSelect2('#parentId', d.groupCompanyId); SetSelect2('#childId', d.plantId); $('#childId').trigger('change');
            if (!$('#autoFilledField').val()) { $('#autoFilledField').val(d.plantCountry); }
        }, 450);
        $('#myModal').modal('show');
    }).fail(function () { ShowLoader(false); ShowToast('Failed to fetch record.', 'error'); });
}

function SaveRecord() {
    if (!ValidateForm()) return;
    var payload = {
        recordId: parseInt($('#recordId').val()) || 0,
        fieldOne: $('#fieldOne').val().trim(),
        fieldTwo: $('#fieldTwo').val(),
        groupCompanyId: parseInt($('#parentId').val()),
        plantId: parseInt($('#childId').val()),
        supplierId: parseInt($('#supplierId').val()),
        currencyId: parseInt($('#currencyId').val()),
        startDate: $('#startDate').val(),
        endDate: $('#endDate').val(),
        amount: 0
    };
    ShowLoader(true);
    $.ajax({
        url: '/TableName/Save', type: 'POST', contentType: 'application/json', data: JSON.stringify(payload), headers: { 'RequestVerificationToken': csrfToken },
        success: function (res) { ShowLoader(false); if (res.success) { $('#myModal').modal('hide'); ShowToast(res.message, 'success'); LoadGrid(); } else { ShowToast(res.message, 'error'); } },
        error: function () { ShowLoader(false); ShowToast('An unexpected error occurred.', 'error'); }
    });
}

function DeleteRecord(id) {
    Swal.fire({ title: 'Are you sure?', text: 'This record will be deleted.', icon: 'warning', showCancelButton: true, confirmButtonColor: '#d33', cancelButtonColor: '#6c757d', confirmButtonText: 'Yes, delete it!' })
        .then(function (result) {
            if (result.isConfirmed) {
                ShowLoader(true);
                $.ajax({
                    url: '/TableName/Delete', type: 'POST', contentType: 'application/json', data: JSON.stringify(id), headers: { 'RequestVerificationToken': csrfToken },
                    success: function (res) { ShowLoader(false); ShowToast(res.message, res.success ? 'success' : 'error'); if (res.success) LoadGrid(); },
                    error: function () { ShowLoader(false); ShowToast('Delete failed.', 'error'); }
                });
            }
        });
}

function ExportToExcel() {
    var params = new URLSearchParams({ groupCompanyId: $('#filterGroupCompany').val() || '', plantId: $('#filterPlant').val() || '', fieldOne: $('#filterFieldOne').val() || '' });
    window.location.href = '/TableName/ExportExcel?' + params.toString();
}

function ValidateForm() {
    var msg = [];
    if (!$('#fieldOne').val().trim()) msg.push('Field One is required.');
    if (!$('#parentId').val()) msg.push('Parent is required.');
    if (!$('#childId').val()) msg.push('Child is required.');
    if (!$('#startDate').val()) msg.push('Start Date is required.');
    if (!$('#endDate').val()) msg.push('End Date is required.');
    if ($('#startDate').val() && $('#endDate').val() && new Date($('#endDate').val()) <= new Date($('#startDate').val())) msg.push('End Date must be after Start Date.');
    if (msg.length > 0) { Swal.fire({ icon: 'warning', title: 'Validation Error', html: msg.map(function (m) { return '• ' + m; }).join('<br/>') }); return false; }
    return true;
}

function ResetForm() {
    $('#myForm')[0].reset(); $('#recordId').val(0); $('#autoFilledField').val('');
    $('.select2-modal').val(null).trigger('change'); $('.is-invalid').removeClass('is-invalid');
}

function ShowToast(message, type) { Swal.fire({ toast: true, position: 'top-end', icon: type, title: message, showConfirmButton: false, timer: 3000, timerProgressBar: true }); }
function ShowLoader(show) {
    if (show && !$('#globalLoader').length) $('body').append('<div id="globalLoader" style="position:fixed;top:0;left:0;width:100%;height:100%;background:rgba(0,0,0,.25);z-index:9999;display:flex;align-items:center;justify-content:center;"><div class="spinner-border text-light" role="status"></div></div>');
    $('#globalLoader').toggle(show);
}
function FormatDate(dateStr) { if (!dateStr) return ''; var d = new Date(dateStr); return String(d.getDate()).padStart(2, '0') + '-' + String(d.getMonth() + 1).padStart(2, '0') + '-' + d.getFullYear(); }
function FormatDateInput(dateStr) { return dateStr ? new Date(dateStr).toISOString().split('T')[0] : ''; }
function SetSelect2(selector, value) { $(selector).val(value).trigger('change.select2'); }
