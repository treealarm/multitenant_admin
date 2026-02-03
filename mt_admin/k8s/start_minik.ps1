# ps стартуем из под админа
# отключаем VPN
minikube start --vm-driver=hyperv --memory=8192 --cpus=2 --disk-size=20g
minikube addons enable metrics-server
minikube addons enable dashboard
#dapr init -k
minikube dashboard --url