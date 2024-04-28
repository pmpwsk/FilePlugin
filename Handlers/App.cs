            default:
                req.Status = 404;
                break;
        }

        return Task.CompletedTask;
    }
}