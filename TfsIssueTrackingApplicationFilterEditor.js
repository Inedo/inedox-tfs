function TfsIssueTrackingApplicationFilterEditor_Init(o) {
    var $ddlCollection = $('#' + o.ddlCollection);
    var $ddlUseWiql = $('#' + o.ddlUseWiql);
    var $ctlWiql = $('#' + o.ctlWiql);
    var $ctlNoWiql = $('#' + o.ctlNoWiql);
    var $ctlProject = $('#' + o.ctlProject);
    var $ctlArea = $('#' + o.ctlArea);

    $ddlUseWiql.change(function () {
        if ($(this).val() == 'True') {
            $ctlWiql.show();
            $ctlNoWiql.hide();
        } else {
            $ctlWiql.hide();
            $ctlNoWiql.show();
        }
    });
    $ddlUseWiql.change();

    $ddlCollection.change(function () {
        var collectionId = $(this).val();
        $.ajax({
            url: o.getProjectsUrl,
            data: { applicationId: o.applicationId, collectionId: collectionId },
            type: 'POST',
            success: function (projects) {
                $ctlProject.select2({
                    data: projects
                });
            }
        });
    });
    $ddlCollection.change();

    $ctlProject.change(function () {
        var projectName = $(this).val();
        var collectionId = $ddlCollection.val();
        $.ajax({
            url: o.getAreasUrl,
            data: { applicationId: o.applicationId, collectionId: collectionId, projectName: projectName },
            type: 'POST',
            success: function (areas) {
                $ctlArea.select2({
                    data: areas,
                    allowClear: true,
                    placeholder: 'no area filter',
                    formatSelection: function (item) {
                        return item.id;
                    },
                    createSearchChoice: function (term) {
                        if (term && /^\\?([^\\]+\\?)+$/.test(term)) {
                            var t = term;
                            if (t[0] == '\\')
                                t = t.substr(1);
                            if (t[t.length - 1] == '\\')
                                t = t.substr(0, t.length - 1);

                            return {
                                id: t,
                                text: t
                            };
                        }

                        return null;
                    }
                });
            }
        });
    });
}